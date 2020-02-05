using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osu_Lyrics.Audio;

namespace osu_Lyrics.Lyrics.Sources
{
    internal class AlsongGetLyric7 : AlsongSource
    {
        public static void Register()
        {
            AddSource<AlsongGetLyric7>();
        }

        public override Task<Lyric> GetLyricAsync(AudioInfo audio) =>
            GetLyricAsync("GetLyric7", $@"<?xml version='1.0' encoding='UTF-8'?>
<SOAP-ENV:Envelope xmlns:SOAP-ENV='http://www.w3.org/2003/05/soap-envelope' xmlns:SOAP-ENC='http://www.w3.org/2003/05/soap-encoding' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:ns2='ALSongWebServer/Service1Soap' xmlns:ns1='ALSongWebServer' xmlns:ns3='ALSongWebServer/Service1Soap12'>
	<SOAP-ENV:Body>
		<ns1:GetLyric7>
			<ns1:encData>7c2d15b8f51ac2f3b2a37d7a445c3158455defb8a58d621eb77a3ff8ae4921318e49cefe24e515f79892a4c29c9a3e204358698c1cfe79c151c04f9561e945096ccd1d1c0a8d8f265a2f3fa7995939b21d8f663b246bbc433c7589da7e68047524b80e16f9671b6ea0faaf9d6cde1b7dbcf1b89aa8a1d67a8bbc566664342e12</ns1:encData>
			<ns1:stQuery>
				<ns1:strChecksum>{audio.CheckSum}</ns1:strChecksum>
				<ns1:strVersion/>
				<ns1:strMACAddress/>
				<ns1:strIPAddress/>
			</ns1:stQuery>
		</ns1:GetLyric7>
	</SOAP-ENV:Body>
</SOAP-ENV:Envelope>");
    }
}