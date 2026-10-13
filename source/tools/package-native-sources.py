import json,pathlib,urllib.request,concurrent.futures,zipfile,subprocess
root=pathlib.Path(__file__).resolve().parent
records=json.loads((root/'heif-packages.json').read_text())
sources=root/'native-sources';sources.mkdir(exist_ok=True)
needed={p.name for p in (root.parent/'assets/heif').glob('*') if p.is_file()}
urls=set()
for record in records:
 if not record.get('url') or not record.get('source'):continue
 archive=root/'msys-packages'/record['url'].split('/')[-1]
 paths=subprocess.check_output(['tar.exe','-tf',str(archive)],text=True).splitlines()
 if any(p.startswith('ucrt64/bin/') and pathlib.PurePosixPath(p).name in needed for p in paths):urls.add(record['source'])
def fetch(url):
 dest=sources/url.split('/')[-1]
 if not dest.exists():
  with urllib.request.urlopen(url,timeout=90) as response,dest.open('wb') as output:
   while chunk:=response.read(1024*1024):output.write(chunk)
 print('Source:',dest.name,flush=True)
with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:list(pool.map(fetch,sorted(urls)))
release=root.parent/'release';release.mkdir(exist_ok=True)
with zipfile.ZipFile(release/'CremaTools-native-sources.zip','w',compression=zipfile.ZIP_STORED) as archive:
 for file in sources.glob('*'):archive.write(file,file.name)
 archive.write(root/'heif-packages.json','packages-and-build-recipes.json')
print('Corresponding HEIC tool and library sources packaged.',flush=True)
