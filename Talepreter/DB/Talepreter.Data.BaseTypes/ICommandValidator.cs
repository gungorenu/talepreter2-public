namespace Talepreter.Data.BaseTypes;

public interface ICommandValidator
{
    void ValidatePreProcess(CommandData commandData);
    void ValidatePreExecute(CommandData commandData);
}
