using System;using System.IO;using System.Runtime.InteropServices;
class IconPatch{
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern IntPtr BeginUpdateResource(string file,bool delete);
 [DllImport("kernel32.dll",SetLastError=true)]static extern bool UpdateResource(IntPtr h,IntPtr type,IntPtr name,ushort lang,byte[] data,uint length);
 [DllImport("kernel32.dll",SetLastError=true)]static extern bool EndUpdateResource(IntPtr h,bool discard);
 static void Main(string[] args){
  var b=File.ReadAllBytes(args[1]);int count=BitConverter.ToUInt16(b,4);
  var group=new MemoryStream();var w=new BinaryWriter(group);w.Write((ushort)0);w.Write((ushort)1);w.Write((ushort)count);
  IntPtr handle=BeginUpdateResource(args[0],false);if(handle==IntPtr.Zero)throw new Exception("Cannot update executable icon");
  try{
   for(int i=0;i<count;i++){
    int p=6+i*16;uint len=BitConverter.ToUInt32(b,p+8),offset=BitConverter.ToUInt32(b,p+12);byte[] image=new byte[len];Array.Copy(b,offset,image,0,len);
    if(!UpdateResource(handle,(IntPtr)3,(IntPtr)(i+1),1033,image,len))throw new Exception("Icon image update failed");
    w.Write(b,p,12);w.Write((ushort)(i+1));
   }
   byte[] data=group.ToArray();if(!UpdateResource(handle,(IntPtr)14,(IntPtr)1,1033,data,(uint)data.Length))throw new Exception("Icon group update failed");
   if(!EndUpdateResource(handle,false))throw new Exception("Icon save failed");
  }catch{EndUpdateResource(handle,true);throw;}
 }
}
