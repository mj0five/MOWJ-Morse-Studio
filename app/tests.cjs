process.chdir(__dirname);
const assert=require('node:assert/strict'),fs=require('node:fs'),vm=require('node:vm');
const Morse=require('./core.js');
const results=[];function test(name,fn){fn();results.push(name);}
test('Every international letter and digit round-trips',()=>{for(const [k,v] of Object.entries({...Morse.EN,...Morse.NUM}))assert.deepEqual(Morse.decode(v),[k]);});
test('SOS is a continuous prosign, not three spaced tokens',()=>{const sos=Morse.tokens(Morse.lessons[0]);assert.equal(sos.length,1);assert.equal(sos[0].code,'...---...');assert.deepEqual(Morse.decode('...---...'),['SOS']);assert.equal(Morse.timeline(sos,1).duration,23);});
test('Audio timeline preserves 1/3/7 unit gaps',()=>{const p=Morse.timeline([{code:'.-',word:0},{code:'.',word:0},{code:'-',word:1}],1);assert.deepEqual(p.events.map(e=>[e.start,e.end]),[[0,1],[2,5],[8,9],[16,19]]);});
test('KeyN works with Persian key value and modifiers stay safe',()=>{assert.equal(Morse.shortcut({code:'KeyN',key:'د'}),'next');assert.equal(Morse.shortcut({code:'KeyN',key:'n',ctrlKey:true}),'');assert.equal(Morse.shortcut({code:'ShiftRight',key:'Shift'}),'clear');});
test('Profile validation and optional values',()=>{assert.equal(Morse.profile().age,'');assert.equal(Morse.profile({callsign:'<test1>',age:130,theme:'bad',gap:20}).callsign,'TEST1');assert.equal(Morse.profile({gap:20}).gap,150);assert.equal(Morse.profile({speed:999}).speed,30);});
test('Personal call signs support portable slash suffixes',()=>{const t=Morse.tokens({text:'W1AW/P'});assert.equal(t[4].code,'-..-.');assert.ok(Morse.timeline(t,1).duration>0);});
let now=0,id=0,timers=new Map(),handlers={},stored=new Map(),nodes=new Map();
const classList=()=>({add(){},remove(){},toggle(){}});
function element(){return {classList:classList(),dataset:{},style:{},value:'',textContent:'',innerHTML:'',children:[],isConnected:true,closest(){return null},setAttribute(){},focus(){},append(e){this.children.push(e)},animate(){},remove(){},reportValidity(){return true}};}
function get(s){if(!nodes.has(s))nodes.set(s,element());return nodes.get(s);}
const document={querySelector:get,querySelectorAll(){return []},createElement:element,body:element(),documentElement:element(),activeElement:element(),addEventListener(k,f){handlers[k]=f}};
class AudioContext{get currentTime(){return now/1000}resume(){return Promise.resolve()}createOscillator(){return {frequency:{},connect(){},start(){}}}createGain(){return {gain:{value:0,cancelScheduledValues(){},setValueAtTime(){},linearRampToValueAtTime(){},setTargetAtTime(){}},connect(){}}}}
function setTimer(f,d){const n=++id;timers.set(n,{at:now+d,f});return n;}function advance(ms){const end=now+ms;let safety=0;while(true){const first=[...timers].filter(([id,t])=>t.at<=end).sort((a,b)=>a[1].at-b[1].at)[0];if(!first)break;if(safety++>10000)throw Error('timer loop');timers.delete(first[0]);now=first[1].at;first[1].f(now);}now=end;}
const context=vm.createContext({console,document,window:{AudioContext,matchMedia(){return {matches:false,addEventListener(){}}},addEventListener(){},scrollTo(){}},localStorage:{getItem:k=>stored.get(k)||null,setItem:(k,v)=>stored.set(k,v),removeItem:k=>stored.delete(k)},performance:{now:()=>now},setTimeout:setTimer,clearTimeout:n=>timers.delete(n),requestAnimationFrame:f=>setTimer(f,16),cancelAnimationFrame:n=>timers.delete(n),FormData,Date});
vm.runInContext(fs.readFileSync('core.js','utf8'),context);
let source=fs.readFileSync('app.js','utf8').replace('render();if(!profile.setup)wizard();','');
vm.runInContext(source,context);const run=s=>vm.runInContext(s,context);
const key=(code,key=code,target=element())=>handlers.keydown({code,key,target,preventDefault(){}});
test('Rapid desk input commits by key time while audio is queued',()=>{run("page='desk';profile.gap=300;profile.boundary='auto';speed=6");for(const code of ['...','---','...']){for(const s of code){run(`addSymbol('${s}')`);advance(20);}advance(310);}assert.equal(run("message.join('')"),'SOS');assert.ok(run('sound.tail')>now/1000);});
test('Manual boundaries do not auto-commit; continuous SOS decodes',()=>{run("clearInput();profile.boundary='manual'");for(const s of '...---...'){run(`addSymbol('${s}')`);advance(15);}advance(1000);assert.equal(run('message.length'),0);key('Enter','Enter');assert.equal(run("message.join('')"),'SOS');});
test('Shift clears pending audio, raw input and received text',()=>{run("addSymbol('.')");key('ShiftLeft','Shift');assert.equal(run('entry'),'');assert.equal(run('message.length'),0);assert.equal(run('sound.events.length'),0);advance(5000);assert.equal(run('message.length'),0);});
test('Practice scoring cannot count the same attempt twice',()=>{run("page='practice';target='A';entry='.-';answered=false");key('Enter');key('Enter');assert.equal(run('score.attempts'),1);assert.equal(run('score.correct'),1);});
test('Next uses physical key even when alphabet selector has focus',()=>{run("page='practice';target='A'");key('KeyN','د',{closest:s=>s==='select'?{}:null});assert.notEqual(run('target'),'A');});
test('Message drill handles wrong attempts, completion and repeat guard',()=>{run("page='onair';lessonId='cq';lessonIndex=0;lessonErrors=0;lessonComplete=false;entry='.';lessonCheck()");assert.equal(run('lessonIndex'),0);assert.equal(run('lessonErrors'),1);run("entry='-.-.';lessonCheck();entry='--.-';lessonCheck()");assert.equal(run('lessonComplete'),true);assert.equal(run('history.at(-1).kind'),'message');assert.equal(run('history.at(-1).ok'),false);const attempts=run('score.attempts');run('lessonCheck()');assert.equal(run('score.attempts'),attempts);});
test('History clear preserves profile and unrelated browser data',()=>{stored.set('other-app','keep');run("profile.callsign='TEST1';profile.setup=true;saveProfile();confirmReset('history')");get('#confirm-reset').onclick();assert.equal(stored.get('other-app'),'keep');assert.equal(JSON.parse(stored.get('mowj-profile')).callsign,'TEST1');assert.equal(run('score.attempts'),0);assert.equal(run('history.length'),0);});
test('Full reset clears only studio keys and restarts setup',()=>{run("confirmReset('all')");get('#confirm-reset').onclick();assert.equal(stored.has('mowj-profile'),false);assert.equal(stored.get('other-app'),'keep');assert.equal(run('profile.setup'),false);assert.equal(run('dialogKind'),'wizard');});
run('stop()');console.log(results.map(x=>'PASS '+x).join('\n'));

