using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Cardsharp
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();
            var info = Application.GetResourceStream(new Uri("/HTML/Instructions.html", UriKind.Relative));
            if (info != null)
                instructionBrowser.NavigateToStream(info.Stream);
            info = Application.GetResourceStream(new Uri("/HTML/License.html", UriKind.Relative));
            if (info != null)
                licenseBrowser.NavigateToStream(info.Stream);
        }
    }
}
