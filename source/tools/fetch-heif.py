import urllib.request,re,hashlib,pathlib,subprocess,concurrent.futures,json,struct,shutil,threading
extract_lock=threading.Lock()
root=pathlib.Path(__file__).resolve().parent
archives=root/'msys-packages';archives.mkdir(exist_ok=True)
extract=root/'msys';extract.mkdir(exist_ok=True)
prefix='mingw-w64-ucrt-x86_64-'
def get(url):
 return urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'CremaTools-build'}),timeout=40).read()
def package(name):
 page=get('https://packages.msys2.org/packages/'+name).decode()
 urls=re.findall(r'https://mirror\.msys2\.org/mingw/ucrt64/[^"<>\s]+?\.pkg\.tar\.zst',page)
 if not urls:
  providers=re.findall(r'href="/packages/('+prefix+r'[^"#]+)"',page)
  providers=[p for p in providers if p!=name]
  if name.endswith('libsharpyuv'):providers=[prefix+'libwebp']
  if name.endswith('-vulkan'):providers=[prefix+'vulkan-loader']
  if providers:return name,providers,{'package':name,'virtual':True}
  raise RuntimeError('No package '+name)
 url=urls[0];dest=archives/url.split('/')[-1]
 expected=re.search(r'SHA256:.*?([a-f0-9]{64})',page,re.S).group(1)
 if not dest.exists():dest.write_bytes(get(url))
 if hashlib.sha256(dest.read_bytes()).hexdigest()!=expected:raise RuntimeError('Hash '+name)
 info=subprocess.check_output(['tar.exe','-xOf',str(dest),'.PKGINFO'],text=True)
 with extract_lock:subprocess.run(['tar.exe','-xf',str(dest),'-C',str(extract),'--exclude','.BUILDINFO','--exclude','.MTREE','--exclude','.PKGINFO'],check=True)
 dependencies=[re.split(r'[<>=]',s)[0] for s in re.findall(r'^depend = (.+)$',info,re.M)]
 sources=re.findall(r'https://mirror\.msys2\.org/mingw/sources/[^"<>\s]+?\.src\.tar\.zst',page)
 print('Downloaded '+name,flush=True)
 return name,dependencies,{'package':name,'url':url,'sha256':expected,'source':sources[0] if sources else '', 'info':info}
queue={prefix+'libheif'};seen=set();records=[]
with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:
 while queue:
  batch=queue-seen;queue=set();seen|=batch
  for name,deps,record in pool.map(package,sorted(batch)):
   records.append(record);queue.update(d for d in deps if d.startswith(prefix) and d not in seen)
def imports(path):
 b=path.read_bytes();p=struct.unpack_from('<I',b,60)[0];count=struct.unpack_from('<H',b,p+6)[0];opt=p+24;osize=struct.unpack_from('<H',b,p+20)[0];magic=struct.unpack_from('<H',b,opt)[0];dirs=opt+(112 if magic==0x20b else 96);rva=struct.unpack_from('<I',b,dirs+8)[0];sections=[]
 for i in range(count):
  o=opt+osize+i*40;vs,va,rs,ro=struct.unpack_from('<IIII',b,o+8);sections.append((va,max(vs,rs),ro))
 def offset(v):
  for va,size,ro in sections:
   if va<=v<va+size:return ro+v-va
  return v
 if not rva:return []
 index=offset(rva);names=[]
 while any(b[index:index+20]):
  nrva=struct.unpack_from('<I',b,index+12)[0];o=offset(nrva);end=b.index(0,o);names.append(b[o:end].decode());index+=20
 return names
available={p.name.lower():p for p in (extract/'ucrt64/bin').glob('*') if p.suffix.lower() in ('.dll','.exe')}
needed={'heif-enc.exe'};resolved=set();out=root.parent/'assets/heif';out.mkdir(parents=True,exist_ok=True)
while needed:
 n=needed.pop();resolved.add(n);p=available[n];shutil.copy2(p,out/p.name)
 needed.update(d.lower() for d in imports(p) if d.lower() in available and d.lower() not in resolved)
licenses=extract/'ucrt64/share/licenses'
if licenses.exists():shutil.copytree(licenses,out/'licenses',dirs_exist_ok=True)
(root/'heif-packages.json').write_text(json.dumps(records,indent=2))
print('HEIC encoder dependencies bundled:',len(resolved),'files',flush=True)
