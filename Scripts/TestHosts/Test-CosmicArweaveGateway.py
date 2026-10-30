import subprocess, pathlib, time, json, urllib.request, socket, sqlite3, hashlib, os, base64
root=pathlib.Path(__file__).resolve().parents[2]
results=root/'TestResults/ArweaveCqlCurrent'
source=root/'LocalState/COSMIC/ArweaveCql/data/db.sqlite'
node=subprocess.check_output(['where.exe','node.exe'],text=True).splitlines()[0]
state=root/'LocalState/COSMIC/ArweaveCql'
probe=state/'lifecycle-probe.json'
pending=state/'lifecycle-probe.pending.json'
assert not pending.exists(), 'Unknown original probe outcome; reconcile, never remint'
address=base64.urlsafe_b64encode(hashlib.sha256(b'COSMIC ArLocal lifecycle probe').digest()).decode().rstrip('=')
expected='12345'
def durable(path,value):
 with path.open('x') as file:file.write(json.dumps(value));file.flush();os.fsync(file.fileno())
def read_balance():
 with urllib.request.urlopen('http://127.0.0.1:18549/wallet/'+address+'/balance',timeout=5) as response:return response.read().decode().strip('"')
observations=[]
for phase in ['first','restart']:
 with socket.socket() as s:s.bind(('127.0.0.1',18549))
 with (results/f'gateway-{phase}.log').open('w') as log:
  p=subprocess.Popen([node,str(root/'Scripts/TestHosts/cosmic-arweave-node.cjs')],cwd=root,stdin=subprocess.PIPE,stdout=log,stderr=log,text=True)
  try:
   for _ in range(100):
    if p.poll() is not None:raise RuntimeError('Owned gateway exited before readiness; preserve source/log')
    try:
     with urllib.request.urlopen('http://127.0.0.1:18549/info',timeout=1) as response:info=json.load(response)
     assert info['network']=='arlocal.N.1'
     break
    except (ConnectionError,urllib.error.URLError):time.sleep(.1)
   else:raise RuntimeError('Owned gateway readiness deadline')
   if not probe.exists():
    assert phase=='first' and read_balance()=='0','Probe source exists without acknowledgement; never remint'
    durable(pending,{'address':address,'balance':expected})
    with urllib.request.urlopen('http://127.0.0.1:18549/mint/'+address+'/'+expected,timeout=5) as response:
     assert response.status==200
    assert read_balance()==expected
    durable(probe,{'address':address,'balance':expected})
    pending.unlink()
   else:
    assert json.loads(probe.read_text())=={'address':address,'balance':expected}
   assert read_balance()==expected,'Original native wallet balance lost; never replace' 
  finally:
   if p.poll() is None:p.stdin.write('stop\n');p.stdin.flush()
   p.wait(timeout=30)
   assert p.returncode==0,'Native teardown failed; preserve process/state'
 assert source.is_file()
 with sqlite3.connect(source.as_uri()+'?mode=ro',uri=True) as db:
  assert db.execute('pragma quick_check').fetchone()[0]=='ok'
  tables=db.execute("select name from sqlite_master where type='table' order by name").fetchall()
  assert ('wallets',) in tables, 'Persistent shutdown dropped native tables'
  assert db.execute('select count(*) from wallets').fetchone()[0]>=1
  snapshot={t[0]:db.execute('select count(*) from "'+t[0].replace('"','""')+'"').fetchone()[0] for t in tables}
 observations.append(dict(phase=phase,rows=snapshot,bytes=source.stat().st_size,sha256=hashlib.sha256(source.read_bytes()).hexdigest()))
assert observations[0]['rows']==observations[1]['rows'],'Native table contents changed across read-only restart'
assert observations[0]['sha256']==observations[1]['sha256'],'Native SQLite bytes changed across read-only restart'
print(json.dumps({'passed':True,'observations':observations}))
