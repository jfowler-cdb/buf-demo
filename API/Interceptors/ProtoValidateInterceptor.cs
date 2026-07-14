using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Interceptors;
using ProtoValidate;

namespace API.Interceptors;

public class ProtoValidateInterceptor : Interceptor
{
    private readonly IValidator _validator;

    public ProtoValidateInterceptor(IValidator validator)
    {
        _validator = validator;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        if (request is IMessage message)
        {
            var result = _validator.Validate(message, failFast: false);
            if (result.Violations.Count > 0)
            {
                var details = string.Join("; ", result.Violations.Select(v =>
                    $"{v.Field}: {v.Message}"));
                throw new RpcException(new Status(StatusCode.InvalidArgument, details));
            }
        }

        return await continuation(request, context);
    }
}
