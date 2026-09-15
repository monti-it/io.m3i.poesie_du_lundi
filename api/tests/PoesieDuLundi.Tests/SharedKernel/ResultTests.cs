using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Tests.SharedKernel;

public class ResultTests
{
    [Fact]
    public void Success_has_no_error()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(string.Empty, result.Error);
    }

    [Fact]
    public void Failure_carries_its_error()
    {
        var result = Result.Failure("that Monday already has a poem");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("that Monday already has a poem", result.Error);
        Assert.Equal(ErrorType.Failure, result.Type);
    }

    [Fact]
    public void NotFound_carries_the_NotFound_type()
    {
        var result = Result.NotFound("Poem not found.");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Type);
    }

    [Fact]
    public void Conflict_carries_the_Conflict_type()
    {
        var result = Result.Conflict("that Monday already has a poem");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Type);
    }

    [Fact]
    public void A_failed_result_must_carry_an_error()
    {
        Assert.Throws<InvalidOperationException>(() => Result.Failure(string.Empty));
    }

    [Fact]
    public void Generic_success_exposes_its_value()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Generic_failure_throws_when_the_value_is_accessed()
    {
        var result = Result.Failure<int>("nope");

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }
}
