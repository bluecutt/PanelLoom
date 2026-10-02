using System.Buffers.Binary;
namespace ComicEditor.Core.Assets;
internal static class ImageHeaders
{
    public static (int Width,int Height) Read(Stream stream)
    {
        var header=new byte[32]; var length=stream.Read(header); stream.Position=0;
        if(length>=24&&header.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10})) return (BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(16,4)),BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(20,4)));
        if(length>=10&&header[0]==71&&header[1]==73&&header[2]==70) return (BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(6,2)),BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(8,2)));
        if(length>=26&&header[0]==66&&header[1]==77) return (BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(18,4)),Math.Abs(BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(22,4))));
        if(length>=2&&header[0]==255&&header[1]==216)
        {
            stream.Position=2;
            while(stream.Position<stream.Length)
            {
                if(stream.ReadByte()!=255) continue;
                int marker; do { marker=stream.ReadByte(); } while(marker==255);
                if(marker<0||marker is 0xD9 or 0xDA) break;
                if(marker==0||marker==0x01||marker is >=0xD0 and <=0xD8) continue;
                var hi=stream.ReadByte(); var lo=stream.ReadByte(); if(lo<0) break; var segmentLength=(hi<<8)|lo;
                if(segmentLength<2||stream.Position+segmentLength-2>stream.Length) break;
                if(marker is >=0xC0 and <=0xCF && marker is not (0xC4 or 0xC8 or 0xCC))
                {
                    _=stream.ReadByte(); var h=(stream.ReadByte()<<8)|stream.ReadByte(); var w=(stream.ReadByte()<<8)|stream.ReadByte(); return (w,h);
                }
                stream.Position+=segmentLength-2;
            }
        }
        if(length>=30&&header.AsSpan(0,4).SequenceEqual("RIFF"u8)&&header.AsSpan(8,4).SequenceEqual("WEBP"u8))
        {
            if(header.AsSpan(12,4).SequenceEqual("VP8X"u8)) return (1+header[24]+(header[25]<<8)+(header[26]<<16),1+header[27]+(header[28]<<8)+(header[29]<<16));
            if(header.AsSpan(12,4).SequenceEqual("VP8L"u8)&&header[20]==0x2f) { var bits=BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(21,4)); return ((int)(bits&0x3fff)+1,(int)((bits>>14)&0x3fff)+1); }
            if(header.AsSpan(12,4).SequenceEqual("VP8 "u8)&&header[23]==0x9d&&header[24]==1&&header[25]==0x2a) return (BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(26,2))&0x3fff,BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(28,2))&0x3fff);
        }
        if(length>=8&&((header[0]==73&&header[1]==73)||(header[0]==77&&header[1]==77)))
        {
            var little=header[0]==73;
            ushort U16(ReadOnlySpan<byte> b)=>little?BinaryPrimitives.ReadUInt16LittleEndian(b):BinaryPrimitives.ReadUInt16BigEndian(b);
            uint U32(ReadOnlySpan<byte> b)=>little?BinaryPrimitives.ReadUInt32LittleEndian(b):BinaryPrimitives.ReadUInt32BigEndian(b);
            if(U16(header.AsSpan(2,2))==42)
            {
                stream.Position=U32(header.AsSpan(4,4)); var countBytes=new byte[2]; stream.ReadExactly(countBytes); var count=U16(countBytes); var w=0; var h=0;
                for(var i=0;i<count;i++) { var entry=new byte[12]; stream.ReadExactly(entry); var tag=U16(entry); if(tag is not (256 or 257)||U32(entry.AsSpan(4,4))!=1) continue; var value=U16(entry.AsSpan(2,2))==3?U16(entry.AsSpan(8,2)):checked((int)U32(entry.AsSpan(8,4))); if(tag==256) w=value; else h=value; }
                if(w>0&&h>0) return(w,h);
            }
        }
        throw new IOException("Unsupported or damaged image header");
    }
}
