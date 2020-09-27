using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetApp.EntityFrameworkCore
{
    public class CSharpNotificationEntityTypeGenerator : CSharpEntityTypeGenerator
    {
        public CSharpNotificationEntityTypeGenerator(ICSharpHelper cSharpHelper) : base(cSharpHelper)
        {
        }

        public override string WriteCode(IEntityType entityType, string @namespace, bool useDataAnnotations)
        {
            return new CSharpEntityTypeCodeEditor { Text = base.WriteCode(entityType, @namespace, useDataAnnotations) }
                .ImplementNotificationEntities(entityType)
                .Text;
        }
    }
}
