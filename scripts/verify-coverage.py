"""Require native collector evidence and 90% line coverage for both packages."""
import json
import shutil
import sys
from pathlib import Path

run_dir, destination = map(Path, sys.argv[1:])
reports = list(run_dir.rglob('coverage.json'))
if not reports:
    raise SystemExit('No new native coverage report was produced.')
# VSTest also copies collector attachments into its TRX deployment folder.
# Accept those identical copies, but reject conflicting coverage evidence.
if len({report.read_bytes() for report in reports}) != 1:
    raise SystemExit('Native coverage reports disagree within this test run.')
report = reports[0]
coverage = json.loads(report.read_text(encoding='utf-8-sig'))
expected = {'ManagedCode.TimeSeries.dll', 'ManagedCode.TimeSeries.Orleans.dll'}
if {Path(module).name for module in coverage} != expected:
    raise SystemExit('Coverage must contain exactly the Core and Orleans modules.')
for module, documents in coverage.items():
    hits = [hit for types in documents.values() for methods in types.values()
            for method in methods.values() for hit in method['Lines'].values()]
    covered = sum(hit > 0 for hit in hits)
    rate = covered / len(hits) if hits else 0
    print(f'{Path(module).name}: {covered}/{len(hits)} lines ({rate:.2%})')
    if rate < 0.9:
        raise SystemExit(f'{module} is below the required 90% line coverage.')
shutil.copy2(report, destination / 'coverage.json')
shutil.copy2(report.with_name('coverage.info'), destination / 'coverage.info')
for trx in run_dir.rglob('*.trx'):
    shutil.copy2(trx, destination / trx.name)
