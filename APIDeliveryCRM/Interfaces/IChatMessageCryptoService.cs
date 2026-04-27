namespace APIDeliveryCRM.Interfaces;

public interface IChatMessageCryptoService
{
    string Encrypt(string plaintext);
    string Decrypt(string payload);
}
