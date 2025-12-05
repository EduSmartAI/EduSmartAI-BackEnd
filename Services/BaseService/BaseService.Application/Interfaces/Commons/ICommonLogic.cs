namespace BaseService.Application.Interfaces.Commons;

public interface ICommonLogic
{
    EncryptTextResponse EncryptText(string beforeEncrypt);
    DecryptTextEmailAndIdResponse DecryptTextEmailAndId(string beforeDecrypt);
    DecryptTextIdAndDateTimeResponse DecryptTextDateTimeAndEmail(string beforeDecrypt);
    string GenerateRandomPassword(int length = 12);
    string GenerateOtp();
}

