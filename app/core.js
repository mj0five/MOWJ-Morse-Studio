/* Pure data and timing helpers. No DOM, audio, storage or network side effects. */
const Morse = (() => {
  const EN={A:'.-',B:'-...',C:'-.-.',D:'-..',E:'.',F:'..-.',G:'--.',H:'....',I:'..',J:'.---',K:'-.-',L:'.-..',M:'--',N:'-.',O:'---',P:'.--.',Q:'--.-',R:'.-.',S:'...',T:'-',U:'..-',V:'...-',W:'.--',X:'-..-',Y:'-.--',Z:'--..'};
  const NUM={'0':'-----','1':'.----','2':'..---','3':'...--','4':'....-','5':'.....','6':'-....','7':'--...','8':'---..','9':'----.'};
  const PUNCT={'/':'-..-.'};
  const FA={'ا':'.-','ب':'-...','پ':'.--.','ت':'-','ث':'-.-..','ج':'.---','چ':'---.','ح':'....','خ':'-.-.-','د':'-..','ذ':'--..','ر':'.-.','ز':'--..','ژ':'--.-','س':'...','ش':'----','ص':'-..-','ض':'..-..','ط':'..-','ظ':'-.--','ع':'.-.-','غ':'--.','ف':'..-.','ق':'...-','ک':'-.-','گ':'--.-','ل':'.-..','م':'--','ن':'-.','و':'.--','ه':'.','ی':'..'};
  const defaults={age:'',callsign:'',theme:'system',speed:16,gap:300,boundary:'auto',setup:false};
  const sources={itu:'https://www.itu.int/rec/R-REC-M.1677-1-200910-I/',arrl:'https://www.arrl.org/w1aw',history:'https://www.arrl.org/inside-w1aw',cwops:'https://cwops.org/wp-content/uploads/2024/01/Practice-Instructions-and-Homework-Assignments-Beginner-CW-Rev-4.2.7.2.pdf'};
  const lessons=[
    {id:'sos',group:'Signals',text:'SOS',prosign:true,description:'International distress signal. Send all nine elements as one continuous signal.',source:'itu'},
    {id:'cq',group:'Signals',text:'CQ',description:'A general call inviting a station to reply. Send C and Q as separate characters.',source:'cwops'},
    {id:'qrs',group:'Signals',text:'QRS',description:'Practice the Q signal used to request slower sending.',source:'cwops'},
    {id:'qrz',group:'Signals',text:'QRZ',description:'Practice the Q signal associated with “Who is calling me?”',source:'cwops'},
    {id:'qth',group:'Signals',text:'QTH',description:'Practice the Q signal associated with station location.',source:'cwops'},
    {id:'w1aw',group:'Call signs',text:'W1AW',description:'ARRL’s Hiram Percy Maxim Memorial Station, known for Morse practice transmissions.',source:'arrl'},
    {id:'w1mk',group:'Call signs',text:'W1MK',description:'Historical ARRL headquarters station call sign, before the W1AW memorial station.',source:'history'},
    {id:'hello',group:'Messages',text:'HELLO',description:'A short plain-text warm-up. Commit each character with Enter.'},
    {id:'test',group:'Messages',text:'TEST 123',description:'A mixed message. Word spaces are added automatically as you progress.'},
    {id:'call',group:'Messages',text:'CQ CQ',description:'Practice a repeated general call with a word boundary.'}
  ];
  function alphabet(lang){return lang==='fa'?FA:lang==='num'?NUM:EN;}
  function decode(code,lang='en'){if(lang==='en'&&code==='...---...')return ['SOS'];return Object.keys({...alphabet(lang),...NUM,...PUNCT}).filter(k=>({...alphabet(lang),...NUM,...PUNCT})[k]===code);}
  function tokens(lesson){if(lesson.prosign)return [{label:'SOS',code:'...---...',word:0}];return Array.from(lesson.text).reduce((a,c)=>{if(c===' '){a.word++;return a;}a.items.push({label:c,code:({...EN,...NUM,...PUNCT})[c],word:a.word});return a;},{items:[],word:0}).items;}
  function timeline(parts,unit){let time=0,events=[],index=0;parts.forEach((part,p)=>{for(let i=0;i<part.code.length;i++){const symbol=part.code[i],duration=unit*(symbol==='.'?1:3);events.push({symbol,start:time,end:time+duration,index:index++,part:p,element:i});time+=duration;if(i<part.code.length-1)time+=unit;}if(p<parts.length-1)time+=unit*(parts[p+1].word!==part.word?7:3);});return {events,duration:time};}
  function shortcut(event){if(event.ctrlKey||event.metaKey||event.altKey||event.repeat)return '';return ({KeyN:'next',KeyR:'replay',ShiftLeft:'clear',ShiftRight:'clear',ArrowLeft:'dot',ArrowRight:'dash',Enter:'enter',NumpadEnter:'enter',Space:'space',Backspace:'back'})[event.code]||({Shift:'clear',ArrowLeft:'dot',ArrowRight:'dash',Enter:'enter',' ':'space'})[event.key]||'';}
  function profile(value={}){return {...defaults,age:Number.isInteger(+value.age)&&+value.age>=1&&+value.age<=120?String(value.age):'',callsign:typeof value.callsign==='string'?value.callsign.toUpperCase().replace(/[^A-Z0-9/]/g,'').slice(0,12):'',theme:['day','night','system'].includes(value.theme)?value.theme:'system',speed:Math.max(6,Math.min(30,+value.speed||16)),gap:Math.max(150,Math.min(1800,+value.gap||300)),boundary:value.boundary==='manual'?'manual':'auto',setup:value.setup===true};}
  return {EN,NUM,FA,defaults,sources,lessons,alphabet,decode,tokens,timeline,shortcut,profile};
})();
if(typeof module!=='undefined')module.exports=Morse;
