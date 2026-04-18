namespace Application.Common.Results;

public class Result
{
    public ResultStatus Status { get; }
    public string Message { get; }

    public bool IsSuccess => Status == ResultStatus.Success;

    protected  Result(ResultStatus status, string message)
    {
        Status = status;
        Message = message;
    }
    
    public static Result Success(string message = "Success")
        => new(ResultStatus.Success, message);
    public static Result Failure(string message, ResultStatus status)
        => new(status, message);
}

public class Result<TDto> : Result
{
    public TDto? Dto { get; }

    protected Result(ResultStatus status, string message, TDto? dto) : base(status, message)
    {
        Dto = dto;
    }
    
    public static Result<TDto> Success(TDto value, string message = "Result with payload is successful.")
        => new (ResultStatus.Success, message, value);
    
    public static Result<TDto> Failure(string message = "Result with payload failed.")
        => new(ResultStatus.Failure, message, default);

    public new static Result<TDto> Failure(string message, ResultStatus status)
        => new(status, message, default);
}