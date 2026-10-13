const fs=require('fs'),crypto=require('crypto'),path=require('path');
const root=path.resolve(__dirname,'..'),version=process.argv[2]||'1.0.0';
const file=path.join(root,'release',`CremaTools Setup ${version}.exe`),bytes=fs.readFileSync(file);
const payload=Buffer.from(JSON.stringify({Version:version,Url:`https://github.com/thibaud-afk/crematools-updates/releases/download/v${version}/CremaTools.Setup.${version}.exe`,Sha256:crypto.createHash('sha256').update(bytes).digest('hex'),Size:bytes.length}));
const signature=crypto.sign('sha256',payload,{key:fs.readFileSync(path.join(__dirname,'keys/update-private.pem')),padding:crypto.constants.RSA_PKCS1_PSS_PADDING,saltLength:32});
fs.mkdirSync(path.join(root,'distribution/updates'),{recursive:true});
fs.writeFileSync(path.join(root,'distribution/updates/stable.json'),JSON.stringify({Payload:payload.toString('base64'),Signature:signature.toString('base64')},null,2));
fs.writeFileSync(path.join(root,'release/SHA256SUMS.txt'),crypto.createHash('sha256').update(bytes).digest('hex')+`  CremaTools.Setup.${version}.exe\n`);
console.log('Signed manifest for CremaTools '+version+' ('+bytes.length+' bytes)');
