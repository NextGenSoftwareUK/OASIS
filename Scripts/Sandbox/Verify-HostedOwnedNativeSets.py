"""Paced owned-fixture native-set acceptance after an exact Development rollout.
One conditional private record per provider; exact acknowledged-revision cleanup.
"""
from pathlib import Path
import argparse
import json
import os
import re
import runpy
import subprocess
import time
import urllib.error
import urllib.request
import uuid

ROOT = Path(__file__).resolve().parents[2]
BASE = "https://dev.api.web4.oasisomniverse.one/api/"
SITE = ROOT.parent / "Web4Site"


def selected_composition_cases(provider, identity):
    name = "COSMIC owned set acceptance " + identity
    description = "COSMIC owned composition " + identity
    source = "SELECT Name, Description FROM " + provider + " WHERE Id = @id ORDER BY Id LIMIT 1"
    parameters = {"id": identity, "description": description}
    return [
        ("cte", "WITH q(Label, Detail) AS (" + source + ") SELECT Label AS Display FROM q WHERE Detail = @description ORDER BY Label LIMIT 1",
            parameters, "data/query-cql/composition", ["Display"], [[name]]),
        ("derived", "SELECT q.Name AS Display FROM (" + source + ") AS q WHERE q.Description = @description ORDER BY q.Name LIMIT 1",
            parameters, "data/query-cql/composition", ["Display"], [[name]]),
        ("cte-set", "WITH q(Label, Detail) AS (" + source + ") (SELECT Label, Detail FROM q) UNION ALL (SELECT Label, Detail FROM q) ORDER BY Label LIMIT 2",
            parameters, "data/query-cql/set", ["Label", "Detail"], [[name, description], [name, description]])]


def compile_owned_composition(compiler, sql, parameters, expected_path, provider, identity):
    program = ("const vm=require('node:vm');const s={module:{exports:{}}};vm.runInNewContext("
        + json.dumps(compiler) + ",s);process.stdout.write(JSON.stringify(s.module.exports.compile("
        + json.dumps(sql) + ",{}," + json.dumps(parameters) + ")));")
    result = subprocess.run(["node", "-"], input=program, capture_output=True, text=True, check=True)
    compiled = json.loads(result.stdout)
    if compiled.get("path") != expected_path or compiled.get("body", {}).get("provider") != provider:
        raise RuntimeError("The exact compiler did not emit the admitted native composition route.")
    def source_ids(value):
        if isinstance(value, dict):
            own = [value.get("value")] if value.get("field") == "Id" and value.get("operator") == "Equal" else []
            return own + [item for child in value.values() for item in source_ids(child)]
        if isinstance(value, list):
            return [item for child in value for item in source_ids(child)]
        return []
    body = compiled["body"]
    sources = [body["left"], body["right"]] if expected_path.endswith("/set") else [body["source"]]
    if any(source.get("take") != 1 or source_ids(source) != [identity] for source in sources):
        raise RuntimeError("Every native source must retain only the one owned identity and its explicit bound.")
    return compiled



def validate_serving_source_review(review, qualified_source, serving_source):
    if (review.get("schema") != "cql-reviewed-serving-source-v1"
            or review.get("qualifiedSource") != qualified_source
            or review.get("servingSource") != serving_source):
        raise RuntimeError("The explicit source review does not bind this qualified producer and serving revision.")
    def git(repo, *arguments):
        return subprocess.run(["git", *arguments], cwd=repo, capture_output=True, text=True, check=True).stdout.strip()
    paths = git(ROOT, "diff", "--name-only", qualified_source, serving_source).splitlines()
    if paths != review.get("rootChangedPaths"):
        raise RuntimeError("The serving root delta differs from its reviewed scope.")
    allowed = {
        "Docker/oasis-dependency-versions.env", "ONODE/NextGenSoftware.OASIS.API.ONODE.Core",
        "Docs/WIP/ONET/ONET_STORAGE_AND_HOLONET_PLAN_2026-11.md",
        "ONODE/TestProjects/NextGenSoftware.OASIS.API.ONODE.Core.IntegrationTests/ONETIntegrationTests.cs",
        "ONODE/TestProjects/NextGenSoftware.OASIS.API.ONODE.Core.UnitTests/ONETDiscoveryTests.cs"}
    if not set(paths).issubset(allowed):
        raise RuntimeError("The serving root delta includes unreviewed runtime changes.")
    for path, expected in review.get("unchangedTrees", {}).items():
        if (git(ROOT, "rev-parse", qualified_source + ":" + path) != expected
                or git(ROOT, "rev-parse", serving_source + ":" + path) != expected):
            raise RuntimeError("A qualified CQL implementation tree or dependency changed.")
    required = {"Providers", "OASIS Architecture/NextGenSoftware.OASIS.API.Core",
        "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI", "WEB6", "ONODE/ONODEManager",
        "STAR ODK", "OASIS Architecture"}
    if set(review.get("unchangedTrees", {})) != required:
        raise RuntimeError("The source review lacks a required CQL implementation tree.")
    dependency = review.get("onodeDependency") or {}
    path = "ONODE/NextGenSoftware.OASIS.API.ONODE.Core"
    before = git(ROOT, "rev-parse", qualified_source + ":" + path)
    after = git(ROOT, "rev-parse", serving_source + ":" + path)
    if before != dependency.get("before") or after != dependency.get("after"):
        raise RuntimeError("The reviewed ONODE dependency pins differ.")
    changed = git(ROOT / path, "diff", "--name-only", before, after).splitlines()
    if changed != dependency.get("changedPaths") or any(
            not item.startswith("ONET/") and item != "Managers/ONETManager.cs" for item in changed):
        raise RuntimeError("The ONODE dependency includes changes outside the reviewed ONET scope.")
    import hashlib
    diff = subprocess.run(["git", "diff", before, after], cwd=ROOT / path, capture_output=True, check=True).stdout
    if hashlib.sha256(diff).hexdigest() != dependency.get("diffSha256"):
        raise RuntimeError("The exact ONET implementation delta differs from its review.")
    return {"qualifiedSource": qualified_source, "servingSource": serving_source,
        "cqlTreesUnchanged": True, "onodeChangesReviewed": True}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--deployment", required=True, type=lambda value: str(uuid.UUID(value)))
    parser.add_argument("--commit", required=True)
    parser.add_argument("--website", required=True)
    parser.add_argument("--resume-before-first-save", action="store_true")
    parser.add_argument("--selected-columns", action="store_true")
    parser.add_argument("--selected-composition", action="store_true")
    parser.add_argument("--release-proof", type=Path)
    parser.add_argument("--serving-source-review", type=Path)

    parser.add_argument("--computed-scalars", action="store_true")
    parser.add_argument("--text-coalesce", action="store_true")
    parser.add_argument("--boolean-text-case", action="store_true")
    parser.add_argument("--boolean-metadata-case", action="store_true")
    parser.add_argument("--combined-metadata-case", action="store_true")
    parser.add_argument("--combined-coalescing", action="store_true")
    parser.add_argument("--providers", nargs="+", choices=["MongoDBOASIS","SQLLiteDBOASIS","Neo4jOASIS"],
        default=["MongoDBOASIS","SQLLiteDBOASIS","Neo4jOASIS"])
    parser.add_argument("--fixture-name", default="HostedOwnedProjectedSetCurrent")
    args = parser.parse_args()
    if any(not re.fullmatch("[0-9a-f]{40}", value) for value in (args.commit, args.website)):
        parser.error("Exact backend and website source commits are required.")
    if args.serving_source_review is not None and not args.selected_composition:
        parser.error("Serving-source reviews are scoped to the selected-composition case.")
    if args.selected_composition:
        if (not args.selected_columns or args.release_proof is None or args.fixture_name == "HostedOwnedProjectedSetCurrent"
                or any((args.computed_scalars,args.text_coalesce,args.boolean_text_case,
                    args.boolean_metadata_case,args.combined_metadata_case,args.combined_coalescing))):
            parser.error("Composition requires selected columns, its explicit release proof and a dedicated fixture name.")
    elif args.release_proof is not None:
        parser.error("An explicit composition release proof is scoped to the composition case.")
    if args.computed_scalars and (not args.selected_columns or any((
            args.text_coalesce,args.boolean_text_case,args.boolean_metadata_case,
            args.combined_metadata_case,args.combined_coalescing))):
        parser.error("Computed scalar qualification uses selected columns and its own release mode.")
    if args.combined_metadata_case and not args.boolean_metadata_case:
        parser.error("Combined metadata release selection requires the Boolean metadata mode.")
    if args.boolean_metadata_case and not args.boolean_text_case:
        parser.error("Boolean metadata qualification uses the conditional text mode.")
    if args.boolean_text_case and (args.text_coalesce or not args.selected_columns):
        parser.error("Conditional text requires selected columns and cannot combine with coalescing.")
    if args.text_coalesce and not args.selected_columns:
        parser.error("Text coalescing uses selected-column qualification and readiness gating.")
    if len(set(args.providers)) != len(args.providers):
        parser.error("Choose each provider once.")
    if not args.selected_columns and args.providers != ["MongoDBOASIS","SQLLiteDBOASIS","Neo4jOASIS"]:
        parser.error("Subset qualification requires selected-column mode.")
    if not re.fullmatch(r"HostedOwnedProjectedSet[A-Za-z0-9]{1,48}", args.fixture_name):
        parser.error("Use a bounded selected-column fixture name without path separators.")
    if args.selected_columns and args.resume_before_first_save:
        parser.error("Selected-column checks use immutable checkpoints; reconcile an interrupted fixture explicitly.")
    if args.combined_coalescing and not args.text_coalesce:
        parser.error("Combined coalescing selects only the qualified coalescing release journal.")
    delivery_name = ("computed-scalars-image-handoff.json" if args.computed_scalars else
        "boolean-metadata-case-combined-image-handoff.json" if args.combined_metadata_case else
        "boolean-metadata-case-image-handoff.json" if args.boolean_metadata_case else
        "boolean-text-case-image-handoff.json" if args.boolean_text_case else
        "text-coalesce-combined-image-handoff.json" if args.combined_coalescing else
        "text-coalesce-image-handoff.json" if args.text_coalesce else
        "projected-set-image-handoff.json" if args.selected_columns else "native-set-image-handoff.json")
    delivery_path = args.release_proof.resolve() if args.selected_composition else ROOT / "LocalState/COSMIC" / delivery_name
    if args.selected_composition and (not delivery_path.is_relative_to((ROOT / "LocalState/COSMIC").resolve()) or not delivery_path.is_file()):
        parser.error("Use an existing exact composition qualification journal in LocalState/COSMIC.")
    delivery = json.loads(delivery_path.read_text())
    if args.selected_composition and not all(delivery.get(key) is True for key in ("compositionAccepted","imageAccepted","packageAccepted")):
        parser.error("Wait for the exact combined-source builds, image and package qualification.")

    if delivery.get("packageAccepted") is not True:
        raise RuntimeError("Wait for immutable producer package acceptance before hosted requests.")
    source_review = None
    if args.serving_source_review is not None:
        review_path = args.serving_source_review.resolve()
        if not review_path.is_relative_to((ROOT / "LocalState/COSMIC").resolve()) or not review_path.is_file():
            parser.error("Use an existing explicit serving-source review inside LocalState/COSMIC.")
        source_review = validate_serving_source_review(json.loads(review_path.read_text()),delivery["source"],args.commit)
    elif delivery["source"] != args.commit:
        raise RuntimeError("Serving revision differs from the qualified producer; an explicit validated scope review is required.")
    helpers = runpy.run_path(str(ROOT / "Scripts/Sandbox/Verify-HostedCqlDemo.py"))
    credentials = runpy.run_path(str(ROOT / "Scripts/Sandbox/HostedSessionCredentials.py"))
    state = ROOT / "LocalState/COSMIC" / (
        args.fixture_name if args.selected_columns else "HostedOwnedNativeSetCurrent")
    if args.selected_columns and state.exists():
        raise RuntimeError("Preserve existing selected-column fixture checkpoints; do not issue another session.")
    journal_path = state / "fixture.json"
    if not args.resume_before_first_save:
        helpers["require_new_fixture"](journal_path)
    cfg = json.loads((Path.home() / ".railway/config.json").read_text())
    rollout_guard = runpy.run_path(str(ROOT / "Scripts/Sandbox/Verify-HostedUnfilteredMapping.py"))["require_current_rollout"]
    def verify_current_rollout():
        request = urllib.request.Request("https://backboard.railway.com/graphql/v2",
            data=json.dumps({"query":"query($id:String!,$s:String!,$e:String!){deployment(id:$id){id status meta} serving:serviceInstance(serviceId:$s,environmentId:$e){latestDeployment{id status meta}}}",
                "variables":{"id":args.deployment,"s":"5123199a-43c4-4439-9921-e10e2aec7841",
                    "e":"a31011a2-d047-4057-b5e8-57d52ebcfefe"}}).encode(),
            headers={"Content-Type":"application/json","Authorization":"Bearer "+cfg["user"]["token"],
                "User-Agent":"railway-cli/5.6.21"})
        with urllib.request.urlopen(request,timeout=30) as response:
            envelope=json.load(response)
        return rollout_guard(envelope,args.deployment,args.commit)
    verify_current_rollout()
    compiler = subprocess.check_output(["git","show",args.website+":cql-text.js"], cwd=SITE).decode()
    state.mkdir(parents=True, exist_ok=True)
    journal = {"source":args.commit,"deployment":args.deployment,"website":args.website,
        "calls":[],"pending":None,"providers":[],"holonWrites":0,"owned":{},"completed":False,\
        "qualifiedProducerSource":delivery["source"],"servingSourceReview":source_review}
    if args.resume_before_first_save:
        if journal_path.with_suffix(".json.new").exists():
            raise RuntimeError("Reconcile the original interrupted checkpoint before resume.")
        journal=json.loads(journal_path.read_text())
        own=journal.get("owned",{}).get("MongoDBOASIS",{})
        if (journal.get("source")!=args.commit or journal.get("deployment")!=args.deployment
                or journal.get("website")!=args.website or journal.get("pending") is not None
                or journal.get("completed") or journal.get("holonWrites")!=0
                or list(journal.get("owned",{}))!=["MongoDBOASIS"] or journal.get("providers")!=[]
                or len(journal.get("calls",[]))!=2
                or journal["calls"][0].get("path")!="sandbox/session"
                or journal["calls"][1].get("path")!="data/query-cql"
                or journal["calls"][1].get("provider")!="MongoDBOASIS"
                or any(c.get("status")!=200 or c.get("isError") for c in journal["calls"])
                or own.get("saveAcknowledged") or own.get("deleteAcknowledged")
                or not own.get("saveOperationId")):
            raise RuntimeError("Only the proven before-first-save checkpoint can resume.")
        uuid.UUID(own["id"]);uuid.UUID(own["saveOperationId"])
    checkpoint = 0
    def write():
        nonlocal checkpoint
        if not args.selected_columns:
            helpers["write"](journal_path, journal)
            return
        checkpoint += 1
        target = state / ("checkpoint-" + str(checkpoint).zfill(4) + ".json")
        with target.open("x", encoding="utf-8") as stream:
            json.dump(journal, stream, indent=2)
            stream.write("\n")
            stream.flush()
            os.fsync(stream.fileno())
        if journal.get("completed"):
            with journal_path.open("x", encoding="utf-8") as stream:
                json.dump(journal, stream, indent=2)
                stream.write("\n")
                stream.flush()
                os.fsync(stream.fileno())
    if not args.resume_before_first_save:
        write()
    token = None
    cookie = None

    def call(path, body, method="POST"):
        if journal["pending"] is not None or len(journal["calls"]) >= (24 if args.selected_composition else 18):
            raise RuntimeError("Pending outcome or request cap; no retry.")
        if journal["calls"]:
            time.sleep(1)
        operation = body.get("operationId") or str(uuid.uuid4())
        if path in {"data/save-holon","data/delete-holon"}:
            body={**body,"setGlobally":False,"autoReplicationMode":"OFF","autoFailOverMode":"OFF",
                "autoLoadBalanceMode":"OFF","showDetailedSettings":False}
        journal["pending"] = {"path":path,"method":method,"operationId":operation}
        write()
        headers = {"Accept":"application/json","Content-Type":"application/json","Idempotency-Key":operation}
        if token:
            headers["Authorization"] = "Bearer "+token
        if cookie:
            headers["Cookie"] = cookie
        request = urllib.request.Request(BASE+path, data=None if method == "GET" else json.dumps(body).encode(), headers=headers, method=method)
        try:
            response = urllib.request.urlopen(request, timeout=40)
        except urllib.error.HTTPError as error:
            response = error
        with response:
            raw = response.read()
            status = response.status
            cookies = response.headers.get_all("Set-Cookie") or []
        if path == "sandbox/session":
            with (state / "issuance.dpapi").open("xb") as stream:
                stream.write(helpers["protected_token"](json.dumps(
                    {"status":status,"body":raw.decode(),"cookies":cookies}).encode()))
        value = helpers["unwrap"](json.loads(raw))
        outcome = {"path":path,"status":status,"isError":value.get("isError",False),"operationId":operation,
            "provider":body.get("provider")}
        code = value.get("errorCode")
        if isinstance(code,str) and re.fullmatch("(?:CQL|COSMIC)_[A-Z_]{2,80}",code):
            outcome["errorCode"] = code
        if status != 200 or value.get("isError"):
            # Preserve the original failed response before interpretation, without
            # leaking provider values or losing the one outcome needed to diagnose.
            with (state / ("failure-"+operation+".dpapi")).open("xb") as stream:
                stream.write(helpers["protected_token"](raw))
        journal["calls"].append(outcome)
        journal["pending"]["outcome"] = outcome
        write()
        if status != 200 or value.get("isError"):
            raise RuntimeError("Definitive response recorded; preserve session and journal, no retry.")
        journal["pending"] = None
        write()
        return value, cookies

    if args.resume_before_first_save:
        bundle=json.loads(helpers["protected_token"]((state/"session.dpapi").read_bytes(),decrypt=True))
        credentials["validate"](bundle,bundle["ownerId"])
        if credentials["needs_renewal"](bundle):
            raise RuntimeError("Original session needs explicit rotation; do not issue another avatar.")
        token,cookie=bundle["jwtToken"],bundle["cookieHeader"]
    else:
        issued, cookies = call("sandbox/session", {})
        avatar = issued["result"]
        bundle = credentials["bundle"](str(uuid.UUID(avatar["id"])), avatar["jwtToken"], cookies)
        token, cookie = bundle["jwtToken"], bundle["cookieHeader"]
        with (state / "session.dpapi").open("xb") as stream:
            stream.write(helpers["protected_token"](json.dumps(bundle).encode()))
    if args.selected_columns:
        catalogue,_=call("data/query-cql/providers?examplesVersion=2",{},method="GET")
        descriptions=catalogue.get("result")
        if not isinstance(descriptions,list):
            raise RuntimeError("Provider discovery did not publish an explicit source list; no fixture save sent.")
        readiness=[]
        for provider in args.providers:
            matches=[row for row in descriptions if isinstance(row,dict) and row.get("provider")==provider]
            if len(matches)!=1:
                raise RuntimeError("Require one unambiguous source description per requested provider.")
            source=matches[0]
            readiness.append({key:source.get(key) for key in ("provider","available","errorCode","activationGeneration","activationHostInstance")})
        journal["readiness"]=readiness
        write()
        if any(row["available"] is not True or row["errorCode"] is not None for row in readiness):
            raise RuntimeError("Requested CQL source startup is incomplete; no fixture save sent.")
    for provider in args.providers:
        resuming=bool(args.resume_before_first_save and provider=="MongoDBOASIS")
        if resuming:
            identity=journal["owned"][provider]["id"]
        else:
            identity=str(uuid.uuid4())
            journal["owned"][provider]={"id":identity,"saveAcknowledged":False,"deleteAcknowledged":False}
            write()
        lookup={"provider":provider,"filters":[{"field":"Id","scalar":"Guid","operator":"Equal","value":identity}],
            "orderBy":"Id","take":1}
        if not resuming:
            absent,_=call("data/query-cql",lookup)
            if absent["result"].get("rows")!=[]:
                raise RuntimeError("New identity already exists; no save sent.")
        save_operation=journal["owned"][provider].get("saveOperationId") or str(uuid.uuid4())
        journal["owned"][provider]["saveOperationId"]=save_operation
        write()
        saved,_=call("data/save-holon",{"providerType":provider,"operationId":save_operation,
            "expectedRevisions":{identity:None},"saveChildren":False,"recursive":False,
            "holon":{"id":identity,"name":"COSMIC owned set acceptance "+identity,
                "isNewHolon":True,"isPublic":False,"isActive":True,
                **({"description":"COSMIC owned composition "+identity} if args.selected_composition else {}),
                "metaData":{"COSMIC.Demo.Dataset":"COSMIC.CQL.SelectedComposition.v1" if args.selected_composition else "COSMIC.CQL.ComputedScalars.v1" if args.computed_scalars else "COSMIC.CQL.BooleanTextCase.v1" if args.boolean_text_case else "COSMIC.CQL.TextCoalesce.v1" if args.text_coalesce else "COSMIC.CQL.NativeSet.v1",
                    **({"coalescedText":None} if args.text_coalesce else {}),
                    **({"flag":True} if args.boolean_metadata_case else {}),
                    **({"flag":True,"stock":None} if args.computed_scalars else {})}}})
        holon=saved.get("result") or {}
        if (saved.get("isSaved") is not True or str(holon.get("id","")).lower()!=identity
                or str(holon.get("createdByAvatarId","")).lower()!=bundle["ownerId"].lower()
                or holon.get("isPublic") is not False):
            raise RuntimeError("Save acknowledgement or ownership incomplete; preserve original fixture.")
        revisions=(holon.get("providerMetaData") or {}).get(provider) or {}
        revision=next((v for k,v in revisions.items() if k.lower()=="cosmic.providerrevisionid"),None)
        if not revision:
            raise RuntimeError("Acknowledged native revision missing; preserve original fixture.")
        revision=str(uuid.UUID(revision))
        journal["owned"][provider].update(saveAcknowledged=True,revision=revision)
        journal["holonWrites"]+=1
        write()
        if args.selected_composition:
            accepted_cases=[]
            for label,sql,parameters,path,aliases,expected in selected_composition_cases(provider,identity):
                compiled=compile_owned_composition(compiler,sql,parameters,path,provider,identity)
                actual,_=call(path,compiled["body"])
                result=actual.get("result") or {}
                expected_version="cql-projected-set-v1" if path.endswith("/set") else "cql-composition-v1"
                if (actual.get("isLoaded") is not True or result.get("version")!=expected_version
                        or result.get("provider")!=provider or result.get("rows")!=expected
                        or [column.get("alias") for column in result.get("columns",[])]!=aliases
                        or path.endswith("/set") and result.get("kind")!="Concat"):
                    raise RuntimeError("Populated native composition output, visibility or visible columns differ; preserve fixture.")
                accepted_cases.append({"case":label,"rows":len(expected),"path":path})
                journal["owned"][provider]["selectedCompositionCases"]=accepted_cases
                write()
            journal["owned"][provider]["sqlCompositionAccepted"]=True
            write()
        else:
            selection = "Name AS Label" if args.selected_columns else "*"
            sql = "(SELECT "+selection+" FROM "+provider+" WHERE Id = @id ORDER BY Id LIMIT 1) UNION ALL (SELECT "+selection+" FROM "+provider+" WHERE Id = @id ORDER BY Id LIMIT 1)"
            parameters={"id":identity}
            if args.text_coalesce:
                parameters["replacement"]="COSMIC replacement "+identity
                sql="SELECT COALESCE(CAST(metadata.coalescedText AS STRING), @replacement) AS Label FROM "+provider+" WHERE Id = @id ORDER BY Id LIMIT 1"
            if args.boolean_text_case:
                parameters["whenTrue"]="COSMIC active "+identity
                parameters["whenFalse"]="COSMIC inactive "+identity
                sql="SELECT CASE WHEN IsActive THEN @whenTrue ELSE @whenFalse END AS Label FROM "+provider+" WHERE Id = @id ORDER BY Id LIMIT 1"
                if args.boolean_metadata_case:
                    sql="SELECT CASE WHEN CAST(metadata.flag AS BOOLEAN) THEN @whenTrue ELSE @whenFalse END AS Label FROM "+provider+" WHERE Id = @id ORDER BY Id LIMIT 1"
            if args.computed_scalars:
                parameters.update(whenTrue="COSMIC active "+identity,whenFalse="COSMIC inactive "+identity,replacement=0)
                sql="SELECT CASE WHEN CAST(metadata.flag AS BOOLEAN) THEN @whenTrue ELSE @whenFalse END AS Label, COALESCE(CAST(metadata.stock AS INT32), @replacement) AS Stock FROM "+provider+" WHERE Id = @id ORDER BY Id LIMIT 1"
            program = "const vm=require('node:vm');const s={module:{exports:{}}};vm.runInNewContext("+json.dumps(compiler)+",s);process.stdout.write(JSON.stringify(s.module.exports.compile("+json.dumps(sql)+",{},"+json.dumps(parameters)+")));"
            result = subprocess.run(["node","-"], input=program, capture_output=True, text=True, check=True)
            compiled = json.loads(result.stdout)
            if args.computed_scalars:
                expected_columns=[
                    {"metadataPath":["flag"],"valueType":"Boolean","nullable":False,"function":"BooleanTextCase",
                        "whenTrue":parameters["whenTrue"],"whenFalse":parameters["whenFalse"],"alias":"Label"},
                    {"metadataPath":["stock"],"valueType":"Int32","nullable":True,"function":"Int32Coalesce",
                        "int32Replacement":parameters["replacement"],"alias":"Stock"}]
                if compiled["path"]!="data/query-cql/projection" or compiled["body"].get("columns")!=expected_columns:
                    raise RuntimeError("Require both exact typed native computed columns before dispatch.")
            elif args.boolean_text_case:
                columns=compiled["body"].get("columns",[])
                if (compiled["path"]!="data/query-cql/projection" or len(columns)!=1
                        or columns[0].get("function")!="BooleanTextCase"
                        or (not args.boolean_metadata_case and columns[0].get("field")!="IsActive")
                        or (args.boolean_metadata_case and (columns[0].get("metadataPath")!=["flag"]
                            or columns[0].get("valueType")!="Boolean" or columns[0].get("nullable") is not False))
                        or columns[0].get("whenTrue")!=parameters["whenTrue"]
                        or columns[0].get("whenFalse")!=parameters["whenFalse"]):
                    raise RuntimeError("Require the exact typed conditional text contract before dispatch.")
            elif args.text_coalesce:
                if (compiled["path"]!="data/query-cql/projection" or len(compiled["body"].get("columns",[]))!=1
                        or compiled["body"]["columns"][0].get("function")!="TextCoalesce"
                        or compiled["body"]["columns"][0].get("nullReplacement")!=parameters["replacement"]):
                    raise RuntimeError("Require the explicit typed native text-coalescing contract before dispatch.")
            elif compiled["path"] != "data/query-cql/set" or compiled["body"]["kind"] != "Concat":
                raise RuntimeError("Website compiler did not produce the expected native set contract.")
            actual, _ = call(compiled["path"], compiled["body"])
            result = actual["result"]
            if result.get("provider") != provider or not (args.text_coalesce or args.boolean_text_case or args.computed_scalars) and result.get("kind") != "Concat":
                raise RuntimeError("Native result lacks exact provenance.")
            if args.computed_scalars:
                columns=result.get("columns") or []
                rows=result.get("rows")
                if (result.get("version")!="cql-projection-v1" or len(columns)!=2
                        or [column.get("alias") for column in columns]!=["Label","Stock"]
                        or rows!=[[parameters["whenTrue"],parameters["replacement"]]]
                        or type(rows[0][1]) is not int):
                    raise RuntimeError("Native computed projection did not return both exact typed cells.")
            elif args.boolean_text_case:
                columns=result.get("columns") or []
                if (result.get("version")!="cql-projection-v1" or len(columns)!=1
                        or columns[0].get("alias")!="Label" or result.get("rows")!=[[parameters["whenTrue"]]]):
                    raise RuntimeError("Native conditional text did not return its exact expected branch.")
            elif args.text_coalesce:
                columns=result.get("columns") or []
                if (result.get("version")!="cql-projection-v1" or len(columns)!=1
                        or columns[0].get("alias")!="Label" or result.get("rows")!=[[parameters["replacement"]]]):
                    raise RuntimeError("Native text coalescing did not return the exact expected explicit replacement.")
            elif args.selected_columns:
                expected = "COSMIC owned set acceptance " + identity
                columns = result.get("columns") or []
                if (result.get("version") != "cql-projected-set-v1" or len(columns) != 1
                        or columns[0].get("alias") != "Label" or result.get("rows") != [[expected],[expected]]):
                    raise RuntimeError("Selected-value set must return exactly the two explicit cells, without hidden fields.")
            elif [str(uuid.UUID(row["id"])) for row in result["rows"]] != [identity,identity]:
                raise RuntimeError("Native entity set occurrence multiplicity differs.")
            journal["owned"][provider]["sqlComputedScalarsAccepted" if args.computed_scalars else "sqlBooleanTextCaseAccepted" if args.boolean_text_case else "sqlCoalesceAccepted" if args.text_coalesce else "sqlSetAccepted"]=True
        delete_operation=str(uuid.uuid4())
        journal["owned"][provider]["deleteOperationId"]=delete_operation
        write()
        call("data/delete-holon",{"id":identity,"providerType":provider,"softDelete":False,
            "operationId":delete_operation,"expectedRevisions":{identity:revision},
            "recursive":False,"loadChildren":False},method="DELETE")
        journal["owned"][provider]["deleteAcknowledged"]=True
        write()
        absent,_=call("data/query-cql",lookup)
        if absent["result"].get("rows")!=[]:
            raise RuntimeError("Exact owned identity absence not verified.")
        journal["owned"][provider]["absenceVerified"]=True
        journal["providers"].append({"provider":provider,"accepted":True,"rows":1 if args.text_coalesce or args.boolean_text_case or args.computed_scalars else 2,"exactCleanupVerified":True})
        write()
    call("avatar/revoke-token", {})
    journal.update(sessionRevoked=True,selectedComposition=args.selected_composition,selectedColumns=args.selected_columns,textCoalesce=args.text_coalesce,booleanTextCase=args.boolean_text_case,booleanMetadataCase=args.boolean_metadata_case,computedScalars=args.computed_scalars,completed=False,
        accepted=all(row["accepted"] for row in journal["providers"]))
    write()
    for name in ("issuance.dpapi","session.dpapi"):
        (state / name).unlink()
    journal["finalRollout"]=verify_current_rollout()
    journal["completed"]=True
    write()
    print(json.dumps({"source":args.commit,"providers":journal["providers"],"calls":len(journal["calls"]),
        "holonWrites":journal["holonWrites"],"selectedColumns":args.selected_columns,"textCoalesce":args.text_coalesce,"booleanTextCase":args.boolean_text_case,"booleanMetadataCase":args.boolean_metadata_case,"computedScalars":args.computed_scalars,"sessionRevoked":True,"credentialsRemoved":True}))

if __name__ == "__main__":
    main()
