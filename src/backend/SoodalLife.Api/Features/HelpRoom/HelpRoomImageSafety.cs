using System.Buffers.Binary;

namespace SoodalLife.Api.Features.HelpRoom;

internal sealed record SafeHelpRoomImage(byte[] Bytes,string Extension,string ContentType,int Width,int Height,string Sha256Hex);

internal static class HelpRoomImageSafety
{
    private const int MaxDimension=12000;
    private const long MaxPixels=40_000_000;

    public static SafeHelpRoomImage Sanitize(byte[] source,string contentType)
    {
        var value=contentType switch
        {
            "image/jpeg"=>SanitizeJpeg(source),
            "image/png"=>SanitizePng(source),
            _=>throw new InvalidDataException("JPEG 또는 PNG 사진만 등록할 수 있습니다.")
        };
        ValidateDimensions(value.Width,value.Height);
        return new(value.Bytes,value.Extension,contentType,value.Width,value.Height,
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(value.Bytes)).ToLowerInvariant());
    }

    private static (byte[] Bytes,string Extension,int Width,int Height) SanitizeJpeg(byte[] source)
    {
        if(source.Length<4||source[0]!=0xff||source[1]!=0xd8)throw Invalid();
        using var output=new MemoryStream(source.Length);output.Write(source,0,2);
        var offset=2;var width=0;var height=0;var ended=false;
        while(offset<source.Length)
        {
            if(source[offset++]!=0xff)throw Invalid();
            while(offset<source.Length&&source[offset]==0xff)offset++;
            if(offset>=source.Length)throw Invalid();
            var marker=source[offset++];
            if(marker==0xd9){output.WriteByte(0xff);output.WriteByte(0xd9);ended=true;break;}
            if(marker is >=0xd0 and <=0xd7 or 0x01){output.WriteByte(0xff);output.WriteByte(marker);continue;}
            if(offset+2>source.Length)throw Invalid();
            var length=BinaryPrimitives.ReadUInt16BigEndian(source.AsSpan(offset,2));
            if(length<2||offset+length>source.Length)throw Invalid();
            if(IsStartOfFrame(marker))
            {
                if(length<7)throw Invalid();
                height=BinaryPrimitives.ReadUInt16BigEndian(source.AsSpan(offset+3,2));
                width=BinaryPrimitives.ReadUInt16BigEndian(source.AsSpan(offset+5,2));
            }
            var metadata=marker is >=0xe0 and <=0xef or 0xfe;
            if(!metadata){output.WriteByte(0xff);output.WriteByte(marker);output.Write(source,offset,length);}
            offset+=length;
            if(marker!=0xda)continue;
            while(offset+1<source.Length)
            {
                if(source[offset]==0xff&&source[offset+1]==0xd9)
                {output.Write(source,offset,2);offset+=2;ended=true;break;}
                output.WriteByte(source[offset++]);
            }
            break;
        }
        if(!ended||width==0||height==0)throw Invalid();
        return(output.ToArray(),".jpg",width,height);
    }

    private static (byte[] Bytes,string Extension,int Width,int Height) SanitizePng(byte[] source)
    {
        ReadOnlySpan<byte> signature=[0x89,0x50,0x4e,0x47,0x0d,0x0a,0x1a,0x0a];
        if(source.Length<signature.Length||!source.AsSpan(0,8).SequenceEqual(signature))throw Invalid();
        using var output=new MemoryStream(source.Length);output.Write(source,0,8);
        var offset=8;var width=0;var height=0;var hasIhdr=false;var hasIdat=false;var ended=false;
        while(offset+12<=source.Length)
        {
            var length=BinaryPrimitives.ReadUInt32BigEndian(source.AsSpan(offset,4));
            if(length>int.MaxValue||offset+12L+length>source.Length)throw Invalid();
            var size=(int)length;var type=System.Text.Encoding.ASCII.GetString(source,offset+4,4);
            var expected=BinaryPrimitives.ReadUInt32BigEndian(source.AsSpan(offset+8+size,4));
            var actual=Crc32(source.AsSpan(offset+4,4+size));if(expected!=actual)throw Invalid();
            if(type=="IHDR")
            {if(hasIhdr||size!=13)throw Invalid();hasIhdr=true;width=BinaryPrimitives.ReadInt32BigEndian(source.AsSpan(offset+8,4));height=BinaryPrimitives.ReadInt32BigEndian(source.AsSpan(offset+12,4));}
            if(type=="IDAT")hasIdat=true;
            if(type is "IHDR" or "PLTE" or "tRNS" or "IDAT" or "IEND")output.Write(source,offset,12+size);
            offset+=12+size;
            if(type=="IEND"){if(size!=0)throw Invalid();ended=true;break;}
        }
        if(!hasIhdr||!hasIdat||!ended)throw Invalid();
        return(output.ToArray(),".png",width,height);
    }

    private static bool IsStartOfFrame(byte marker)=>marker is 0xc0 or 0xc1 or 0xc2 or 0xc3 or 0xc5 or 0xc6 or 0xc7 or 0xc9 or 0xca or 0xcb or 0xcd or 0xce or 0xcf;
    private static uint Crc32(ReadOnlySpan<byte> data){var crc=0xffffffffu;foreach(var value in data){crc^=value;for(var bit=0;bit<8;bit++)crc=(crc&1)!=0?0xedb88320u^(crc>>1):crc>>1;}return~crc;}
    private static void ValidateDimensions(int width,int height){if(width<=0||height<=0||width>MaxDimension||height>MaxDimension||(long)width*height>MaxPixels)throw new InvalidDataException("사진 해상도가 허용 범위를 초과합니다.");}
    private static InvalidDataException Invalid()=>new("손상되었거나 안전하게 처리할 수 없는 이미지입니다.");
}
