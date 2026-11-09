using System.Drawing;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EfCoreMiniApp.Data;

public class ColorToStringConverter : ValueConverter<Color, string>
{
    public ColorToStringConverter()
        : base(color => color.Name, name => Color.FromName(name))
    {
    }
}
