using SharedKernel;

namespace Domain.Curriculum;

public static class ModuleErrors
{
    public static Error NotFound(Guid moduleId) => Error.NotFound(
        "Modules.NotFound",
        $"The module with the Id = '{moduleId}' was not found");
}
