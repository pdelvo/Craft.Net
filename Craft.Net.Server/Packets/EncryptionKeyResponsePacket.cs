using System;
using javax.crypto;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using javax.crypto.spec;
using System.Linq;

namespace Craft.Net.Server
{
    public class EncryptionKeyResponsePacket : Packet
    {
        public byte[] SharedSecret, VerifyToken;

        public EncryptionKeyResponsePacket()
        {
            SharedSecret = new byte[0];
            VerifyToken = new byte[0];
        }

        public override byte PacketID
        {
            get
            {
                return 0xFC;
            }
        }

        public override int TryReadPacket(byte[] Buffer, int Length)
        {
            short secretLength = 0, verifyLength = 0;
            int offset = 1;
            if (!TryReadShort(Buffer, ref offset, out secretLength))
                return -1;
            if (!TryReadArray(Buffer, secretLength, ref offset, out this.SharedSecret))
                return -1;
            if (!TryReadShort(Buffer, ref offset, out verifyLength))
                return -1;
            if (!TryReadArray(Buffer, verifyLength, ref offset, out this.VerifyToken))
                return -1;
            return offset;
        }

        public override void HandlePacket(MinecraftServer Server, ref MinecraftClient Client)
        {
            Cipher cipher = Cipher.getInstance("RSA");
            cipher.init(Cipher.DECRYPT_MODE, Server.KeyPair.getPrivate());
            Client.SharedKey = new SecretKeySpec(cipher.doFinal(SharedSecret), "AES-128");

            Client.Encrypter = new BufferedBlockCipher(new CfbBlockCipher(new AesEngine(), 8));
            Client.Encrypter.Init(true,
                   new ParametersWithIV(new KeyParameter(Client.SharedKey.getEncoded()), 
                   Client.SharedKey.getEncoded(), 0, 16));

            Client.Decrypter = new BufferedBlockCipher(new CfbBlockCipher(new AesEngine(), 8));
            Client.Decrypter.Init(false,
                   new ParametersWithIV(new KeyParameter(Client.SharedKey.getEncoded()), 
                   Client.SharedKey.getEncoded(), 0, 16));

            Client.SendPacket(new EncryptionKeyResponsePacket());
            Server.ProcessSendQueue();
        }

        public override void SendPacket(MinecraftServer Server, MinecraftClient Client)
        {
            // Send packet and enable encryption
            byte[] buffer = new byte[] { PacketID }.Concat(
                CreateShort((short)SharedSecret.Length)).Concat(
                SharedSecret).Concat(
                CreateShort((short)VerifyToken.Length)).Concat(
                VerifyToken).ToArray();
            Client.SendData(buffer);
            Client.EncryptionEnabled = true;
        }
    }
}