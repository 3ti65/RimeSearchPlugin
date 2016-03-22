using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using RimeCommon.Messaging;
using RimeCommon.Plugins;
using RimeCommon.Plugins.Messages;
using RimeCommon.VFS;
using RimeCommon.VFS.Backends;
using RimeLib.Frostbite.Bundles.Entries;

namespace EbxSearchPlugin
{
    public class SearchPlugin : RimePlugin
    {
        public override string Name => "SearchPlugin";
        public override string Author => "";
        public override string Version => "1.0";
        public override string Extension => "_ebx-search-plugin";
        public override MountPoint Mount => MountPoint.Left;
        public override bool MultiInstance => false;
        public override UserControl MainControl => m_Control;

        private SearchControl m_Control;
        private Task m_SearchTask = new Task(() => { });
        private readonly CancellationTokenSource m_TokenSource = new CancellationTokenSource();
        public override void Init(params object[] p_Parameters)
        {
            
        }

        public override void InitControl()
        {
            // Create our new control
            m_Control = new SearchControl();

            // Register to the events
            m_Control.txtSearch.TextChanged += OnTextChanged;
            m_Control.lstView.MouseDoubleClick += OnMouseDoubleClicked;
        }

        private void OnMouseDoubleClicked(object p_Sender, MouseButtonEventArgs p_E)
        {
            var s_ListView = p_Sender as ListView;

            var s_SelectedItem = s_ListView?.SelectedItem as ResultViewModel;
            if (s_SelectedItem == null)
                return;

            MessageManager.SendMessage(new PluginRequestLoadMessage
            {
                Extension = ".ebx",
                Arguments = new object[]{ s_SelectedItem.Entry } 
            });
        }

        private async void OnTextChanged(object p_Sender, TextChangedEventArgs p_E)
        {
            var s_TextBox = p_Sender as TextBox;

            var s_Text = s_TextBox?.Text;

            if (string.IsNullOrWhiteSpace(s_Text))
                return;

            m_SearchTask = Task.Run(() =>
            {
                var s_List = Search(s_Text);

                m_Control.Dispatcher.Invoke(() =>
                {
                    m_Control.lstView.ItemsSource = s_List;
                });
            }, m_TokenSource.Token);

            try
            {
                await m_SearchTask;
            }
            catch
            {
                // ignored
            }
        }

        public class ResultViewModel
        {
            public EntryBase Entry { get; set; }
            public string Path { get; set; }

            public override string ToString()
            {
                return Path;
            }
        }
        IEnumerable<ResultViewModel> Search(string p_SearchString)
        {
            var s_Backend = (BundleBackend) FileSystem.GetBackend("/bundles");
            if (s_Backend == null)
                return null;

            Dictionary<string, EntryBase> s_Items;
            s_Backend.Search(p_SearchString, out s_Items);

            return s_Items.Select(p_Pair => new ResultViewModel
            {
                Entry = p_Pair.Value, Path = p_Pair.Key
            }).ToList();
        }
    }
}
