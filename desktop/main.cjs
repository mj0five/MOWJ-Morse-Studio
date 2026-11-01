'use strict';
const {app,BrowserWindow,Menu,session,protocol,net,shell,dialog}=require('electron');
const fs=require('fs'),path=require('path'),{pathToFileURL}=require('url');
protocol.registerSchemesAsPrivileged([{scheme:'mowj',privileges:{standard:true,secure:true,supportFetchAPI:true}}]);
app.setName('MOWJ Morse Studio');
const qaData=process.env.MOWJ_QA_USER_DATA;
app.setPath('userData',qaData&&fs.existsSync(path.join(qaData,'.mowj-qa'))?qaData:path.join(app.getPath('appData'),'MOWJ Morse Studio'));
if(!app.requestSingleInstanceLock()){app.quit();}else{
let win;
app.on('second-instance',()=>{if(win){if(win.isMinimized())win.restore();win.show();win.focus();}});
app.on('window-all-closed',()=>app.quit());
app.whenReady().then(()=>{
 app.setAppUserModelId('MOWJ.MorseStudio');
 protocol.handle('mowj',request=>{
  const u=new URL(request.url);
  if(u.hostname!=='studio'||!['/','/index.html'].includes(u.pathname))return new Response('Not found',{status:404});
  return net.fetch(pathToFileURL(path.join(__dirname,'index.html')).toString());
 });
 const ses=session.defaultSession;
 ses.setPermissionRequestHandler((_wc,_permission,cb)=>cb(false));
 ses.setPermissionCheckHandler(()=>false);
 ses.webRequest.onBeforeRequest({urls:['http://*/*','https://*/*','ws://*/*','wss://*/*']},(_details,cb)=>cb({cancel:true}));
 ses.on('will-download',(_event,item)=>{
  const ext=path.extname(item.getFilename()).toLowerCase();
  if(ext!=='.pdf'){item.cancel();return;}
  item.setSaveDialogOptions({title:'Save mission PDF',defaultPath:path.join(app.getPath('downloads'),path.basename(item.getFilename())),filters:[{name:'PDF document',extensions:['pdf']}]});
 });
 win=new BrowserWindow({width:1440,height:960,minWidth:1000,minHeight:700,show:false,backgroundColor:'#101419',title:'MOWJ — Morse Studio',icon:path.join(__dirname,'mowj.ico'),autoHideMenuBar:true,webPreferences:{nodeIntegration:false,nodeIntegrationInSubFrames:false,contextIsolation:true,sandbox:true,webSecurity:true,spellcheck:false}});
 win.removeMenu();
 Menu.setApplicationMenu(null);
 const external=url=>{try{if(new URL(url).protocol==='https:')shell.openExternal(url);}catch{}};
 win.webContents.setWindowOpenHandler(({url})=>{external(url);return {action:'deny'}});
 win.webContents.on('will-navigate',(event,url)=>{if(!url.startsWith('mowj://studio/')){event.preventDefault();external(url);}});
 win.webContents.on('will-attach-webview',event=>event.preventDefault());
 win.webContents.on('render-process-gone',()=>dialog.showErrorBox('MOWJ','The display process stopped. Close and reopen MOWJ; your saved progress remains available.'));
 win.webContents.on('did-fail-load',(_e,code,message,_url,main)=>{if(main&&code!==-3)dialog.showErrorBox('MOWJ could not open',message)});
 win.on('unresponsive',()=>{});
 win.once('ready-to-show',()=>{win.show();win.maximize()});
 win.loadURL('mowj://studio/index.html');
}).catch(error=>{dialog.showErrorBox('MOWJ startup error',String(error));app.quit()});
}
