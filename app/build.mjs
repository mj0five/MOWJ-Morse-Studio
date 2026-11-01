import { readFile, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
const read = name => readFile(new URL(name, import.meta.url), 'utf8');
const [html,css,core,app,storySource,rhythm] = await Promise.all(['index.html','style.css','core.js','app.js','story-missions.html','rhythm.js'].map(read));
let story=storySource;
for(const name of ["coast.png", "helicopter.png", "wreck.png", "lifeboat.png"]){
 const bytes=await readFile(new URL('assets/story/'+name,import.meta.url));
 story=story.replaceAll('assets/story/'+name,'data:image/png;base64,'+bytes.toString('base64'));
}
const storyScript='const MOWJ_STORY_HTML = '+JSON.stringify(story)+';';
await writeFile(new URL('story.js',import.meta.url),storyScript,'utf8');
const inline = js => `<script>${js.replace(/<\/script/gi,'<\\/script')}</script>`;
const projectLicense=await readFile(new URL('../LICENSE',import.meta.url),'utf8');
const licensedHtml=html.replace('</head>',()=>`<!-- MOWJ project license\n${projectLicense}\n-->\n</head>`);
const standalone=licensedHtml.replace('<link rel="stylesheet" href="style.css">',()=>`<style>${css}</style>`).replace('<script src="core.js"></script>',()=>inline(core)).replace('<script src="story.js"></script>',()=>inline(storyScript)).replace('<script src="rhythm.js"></script>',()=>inline(rhythm)).replace('<script src="app.js"></script>',()=>inline(app));
const output = new URL('../mowj-offline.html',import.meta.url);
await writeFile(output,standalone,'utf8');
console.log(`Offline app ready: ${fileURLToPath(output)}`);
