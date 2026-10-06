#!/usr/bin/env python3
"""Generate XmlSerializer DTOs from the pinned production ARCA WSDL snapshots."""

from __future__ import annotations

import argparse
import os
import re
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SOURCES = ROOT / "docs/reference/contracts"
OUTPUT = ROOT / "src/NetArcaWs/Contracts/Generated"
SERVICES_OUTPUT = ROOT / "src/NetArcaWs/Services/Generated/ArcaServices.g.cs"
XSD_NS = "http://www.w3.org/2001/XMLSchema"
SERVICES = (
    ("wsfev1-production.wsdl", "WsfeV1", "Wsfev1Service", "wsfe", "Wsfe"),
    ("wsfexv1-production.wsdl", "WsfexV1", "Wsfexv1Service", "wsfex", "Wsfex"),
    ("wsmtxca-production.wsdl", "Wsmtxca", "Wsmtxcav1Service", "wsmtxca", "Wsmtxca"),
    ("padron-a4-production.wsdl", "PadronA4", "PadronA4Service", "ws_sr_padron_a4", "PadronA4"),
    ("padron-a5-production.wsdl", "PadronA5", "PadronA5Service", "ws_sr_constancia_inscripcion", "PadronA5"),
    ("padron-a10-production.wsdl", "PadronA10", "PadronA10Service", "ws_sr_padron_a10", "PadronA10"),
    ("padron-a13-production.wsdl", "PadronA13", "PadronA13Service", "ws_sr_padron_a13", "PadronA13"),
)


def inline_schema(path: Path, destination: Path) -> str:
    source_text = path.read_text(encoding="utf-8-sig")
    root = ET.fromstring(source_text)
    schema = root.find(f".//{{{XSD_NS}}}schema")
    if schema is None:
        raise SystemExit(f"{path.name}: no inline XML Schema was found")
    if schema.find(f"{{{XSD_NS}}}import") is not None or schema.find(f"{{{XSD_NS}}}include") is not None:
        raise SystemExit(f"{path.name}: external schema imports/includes need to be pinned first")

    # XSD QName-valued attributes (type/base/ref) keep lexical prefixes. Preserve
    # all WSDL-level namespace declarations when extracting the nested schema.
    definitions = re.search(r"<(?:[A-Za-z_][\w.-]*:)?definitions\b[^>]*>", source_text)
    if definitions is None:
        raise SystemExit(f"{path.name}: no WSDL definitions element was found")
    start = definitions.group(0)
    for prefix, uri in re.findall(r'xmlns:([A-Za-z_][\w.-]*)=["\']([^"\']+)["\']', start):
        schema.set(f"xmlns:{prefix}", uri)
    default_namespace = re.search(r'xmlns=["\']([^"\']+)["\']', start)
    if default_namespace:
        schema.set("xmlns", default_namespace.group(1))

    destination.parent.mkdir(parents=True, exist_ok=True)
    ET.ElementTree(schema).write(destination, encoding="utf-8", xml_declaration=True)
    target_namespace = schema.get("targetNamespace")
    if not target_namespace:
        raise SystemExit(f"{path.name}: inline XML Schema has no targetNamespace")
    return target_namespace


def wsdl_operation_types(root: ET.Element, generated_source: str) -> list[tuple[str, str, str, str]]:
    ns = {
        "wsdl": "http://schemas.xmlsoap.org/wsdl/",
        "soap": "http://schemas.xmlsoap.org/wsdl/soap/",
    }
    root_types = {
        match.group(1): match.group(2)
        for match in re.finditer(
            r'XmlRootAttribute\("([^\"]+)"[^\]]*\)\]\s+public partial class (\w+)',
            generated_source,
            re.S,
        )
    }
    messages = {element.get("name"): element for element in root.findall("wsdl:message", ns)}
    port_type = root.find("wsdl:portType", ns)
    if port_type is None:
        raise SystemExit("WSDL has no portType")

    actions: dict[str, str] = {}
    for binding in root.findall("wsdl:binding", ns):
        if binding.find("soap:binding", ns) is None:
            continue
        for operation in binding.findall("wsdl:operation", ns):
            soap_operation = operation.find("soap:operation", ns)
            if soap_operation is not None:
                actions[operation.get("name", "")] = soap_operation.get("soapAction", "")

    result = []
    for operation in port_type.findall("wsdl:operation", ns):
        name = operation.get("name", "")
        input_element = operation.find("wsdl:input", ns)
        output_element = operation.find("wsdl:output", ns)
        if input_element is None or output_element is None:
            raise SystemExit(f"{name}: one-way operations are not supported")
        request_message = messages.get(input_element.get("message", "").split(":")[-1])
        response_message = messages.get(output_element.get("message", "").split(":")[-1])
        if request_message is None or response_message is None:
            raise SystemExit(f"{name}: input/output WSDL messages are missing")
        request_part = request_message.find("wsdl:part", ns)
        response_part = response_message.find("wsdl:part", ns)
        if response_part is None:
            raise SystemExit(f"{name}: output WSDL message part is missing")
        request_root = request_part.get("element", "").split(":")[-1] if request_part is not None else ""
        response_root = response_part.get("element", "").split(":")[-1]
        request_type = root_types.get(request_root) if request_part is not None else None
        response_type = root_types.get(response_root)
        if (request_part is not None and request_type is None) or response_type is None:
            raise SystemExit(f"{name}: generated root type is missing ({request_root}, {response_root})")
        result.append((name, request_type, response_type, actions.get(name, "")))
    return result


def render_services() -> str:
    lines = [
        "// <auto-generated>",
        "// Generated from pinned ARCA production WSDLs by scripts/generate-contracts.sh.",
        "// Do not edit manually; regenerate after reviewing source WSDL changes.",
        "// </auto-generated>",
        "#nullable disable",
        "using NetArcaWs.HealthChecks;",
        "using NetArcaWs.Multitenancy;",
        "using NetArcaWs.Transport;",
    ]
    lines.extend(["", "namespace NetArcaWs.Services;", ""])
    for wsdl_name, contract_ns, client_name, service_id, endpoint_prefix in SERVICES:
        wsdl = ET.parse(SOURCES / wsdl_name).getroot()
        source = (OUTPUT / f"{contract_ns}.g.cs").read_text(encoding="utf-8")
        operations = wsdl_operation_types(wsdl, source)
        interface_name = f"I{client_name}"
        methods: list[str] = []
        implementations: list[str] = []
        for operation_name, request_type, response_type, action in operations:
            request_type = f"global::NetArcaWs.Contracts.{contract_ns}.{request_type}" if request_type else None
            response_type = f"global::NetArcaWs.Contracts.{contract_ns}.{response_type}"
            if operation_name.lower().endswith("dummy"):
                request_type = request_type or "object"
                request_expression = f"new {request_type}()" if request_type != "object" else "null!"
                signature = (
                    f"Task<{response_type}> {operation_name}Async(ArcaEnvironment environment = ArcaEnvironment.Homologation, "
                    "CancellationToken cancellationToken = default);"
                )
                implementations.extend(
                    [
                        f"    public {signature[:-1]}",
                        f"        => SendUnauthenticatedAsync<{request_type}, {response_type}>(",
                        f'            ArcaServiceEndpoints.Select(environment, ArcaServiceEndpoints.{endpoint_prefix}Homologation, ArcaServiceEndpoints.{endpoint_prefix}Production), "{action}", {request_expression}, cancellationToken);',
                        "",
                    ]
                )
            else:
                signature = (
                    f"Task<{response_type}> {operation_name}Async(ArcaTenantContext tenant, {request_type} request, "
                    "CancellationToken cancellationToken = default);"
                )
                implementations.extend(
                    [
                        f"    public {signature[:-1]}",
                        f"        => SendAuthenticatedAsync<{request_type}, {response_type}>(TicketService,",
                        f"            ArcaServiceEndpoints.Select(tenant.Environment, ArcaServiceEndpoints.{endpoint_prefix}Homologation, ArcaServiceEndpoints.{endpoint_prefix}Production),",
                        f'            "{action}", tenant, request, cancellationToken);',
                        "",
                    ]
                )
            methods.append(f"    {signature}")

        lines.append(f"public interface {interface_name}")
        lines.append("{")
        lines.extend(methods)
        lines.append("}")
        lines.append("")
        lines.append(f"public sealed class {client_name} : ArcaSoapServiceBase, {interface_name}")
        lines.append("{")
        lines.append(f'    public const string TicketService = "{service_id}";')
        lines.append("")
        lines.append(f"    public {client_name}(ISoapTransport transport, IArcaTicketProvider tickets) : base(transport, tickets) {{ }}")
        lines.append("")
        lines.extend(implementations)
        lines.append("}")
        lines.append("")
    return "\n".join(lines)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--xscgen", required=True, type=Path, help="Pinned xscgen executable")
    parser.add_argument("--output", type=Path, default=OUTPUT)
    args = parser.parse_args()
    if not args.xscgen.is_file():
        raise SystemExit(f"xscgen executable not found: {args.xscgen}")

    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="netarca-contracts-") as temporary:
        temp = Path(temporary)
        generated_files: list[tuple[str, Path]] = []
        for wsdl_name, service, _, _, _ in SERVICES:
            wsdl = SOURCES / wsdl_name
            if not wsdl.is_file():
                raise SystemExit(f"Missing pinned WSDL: {wsdl}")
            schema_file = temp / f"{service}.xsd"
            target_namespace = inline_schema(wsdl, schema_file)
            service_output = temp / service
            service_output.mkdir()
            subprocess.run(
                [
                    str(args.xscgen),
                    f"--namespace={target_namespace}=NetArcaWs.Contracts.{service}",
                    "--nullable",
                    "--order",
                    "--netCore",
                    "--commandArgs-",
                    f"--output={service_output}",
                    str(schema_file),
                ],
                check=True,
            )
            files = sorted(service_output.glob("*.cs"))
            if len(files) != 1:
                raise SystemExit(f"{service}: expected one generated file, got {len(files)}")
            generated_files.append((f"{service}.g.cs", files[0]))

        for destination_name, generated in generated_files:
            content = "\n".join(line.rstrip() for line in generated.read_text(encoding="utf-8").splitlines()) + "\n"
            # Generated XSD classes intentionally model absent required response
            # fields as null; runtime nullability is enforced by service callers.
            destination = output / destination_name
            temporary_output = destination.with_suffix(destination.suffix + ".tmp")
            temporary_output.write_text("#nullable disable\n" + content, encoding="utf-8")
            os.replace(temporary_output, destination)

    SERVICES_OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    service_content = render_services()
    temporary_services = SERVICES_OUTPUT.with_suffix(SERVICES_OUTPUT.suffix + ".tmp")
    temporary_services.write_text(service_content, encoding="utf-8")
    os.replace(temporary_services, SERVICES_OUTPUT)


if __name__ == "__main__":
    main()
