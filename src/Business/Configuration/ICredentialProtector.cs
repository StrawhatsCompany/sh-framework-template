namespace Business.Configuration;

public interface ICredentialProtector
{
    string Protect(string plaintext);
    string Unprotect(string ciphertext);
}
