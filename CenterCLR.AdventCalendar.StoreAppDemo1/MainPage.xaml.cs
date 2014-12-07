// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=391641 を参照してください
using CenterCLR.Sgml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace CenterCLR.AdventCalendar.StoreAppDemo1
{
	/// <summary>
	/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
	/// </summary>
	public sealed partial class MainPage : Page
	{
		public MainPage()
		{
			this.InitializeComponent();

			this.NavigationCacheMode = NavigationCacheMode.Required;

			this.Items = new ObservableCollection<ItemModel>();
			this.DataContext = this;
		}

		private static string GetAttribute(XElement element, string name)
		{
			var attribute = element.Attribute(name);
			return (attribute != null) ? attribute.Value : null;
		}

		private static Uri ParseUrl(Uri baseUrl, string url)
		{
			Uri result;
			Uri.TryCreate(baseUrl, url, out result);
			return result;
		}

		/// <summary>
		/// 指定されたURLのHTMLを読み取って解析します。
		/// </summary>
		/// <param name="url">URL</param>
		/// <returns>コンテンツ群のURL</returns>
		private static async Task<IReadOnlyList<KeyValuePair<Uri, string>>> LoadFromAsync(Uri url)
		{
			using (var client = new HttpClient())
			{
				using (var stream = await client.GetStreamAsync(url).ConfigureAwait(false))
				{
					// SgmlReaderを使う
					var sgmlReader = new SgmlReader(stream);

					var document = XDocument.Load(sgmlReader);

					// 郵便番号データダウンロードのサイトをスクレイピングする
					// ターゲットは、html/body/div[id=wrap-inner]/div[id=main-box]/div[class=pad]/table/tbody/tr/td/a
					// にあるhrefとなる。
					// パースとトラバースまでワーカースレッドで実行しておく。
					return
						(from html in document.Elements("html")
						 from body in html.Elements("body")
						 from divWrapOuter in body.Elements("div")
						 let wrapOuter = GetAttribute(divWrapOuter, "id")
						 where wrapOuter == "wrap-outer"
						 from divWrapInner in divWrapOuter.Elements("div")
						 let wrapInner = GetAttribute(divWrapInner, "id")
						 where wrapInner == "wrap-inner"
						 from divMainBox in divWrapInner.Elements("div")
						 let mainBox = GetAttribute(divMainBox, "id")
						 where mainBox == "main-box"
						 from divPad in divMainBox.Elements("div")
						 let pad = GetAttribute(divPad, "class")
						 where pad == "pad"
						 from table in divPad.Elements("table")
						 from tbody in table.Elements("tbody")
						 from tr in tbody.Elements("tr")
						 from td in tr.Elements("td")
						 from a in td.Elements("a")
						 let href = GetAttribute(a, "href")
						 where href.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true
						 let zipUrl = ParseUrl(url, href)
						 where zipUrl != null
						 let text = a.Value
						 where string.IsNullOrWhiteSpace(text) == false
						 select new KeyValuePair<Uri, string>(zipUrl, text)).
						ToList();
				}
			}
		}

		/// <summary>
		/// このページがフレームに表示されるときに呼び出されます。
		/// </summary>
		/// <param name="e">このページにどのように到達したかを説明するイベント データ。
		/// このプロパティは、通常、ページを構成するために使用します。</param>
		protected override async void OnNavigatedTo(NavigationEventArgs e)
		{
			// 「郵便番号データダウンロード」スクレイピングを非同期で実行
			var urls = await LoadFromAsync(new Uri("http://www.post.japanpost.jp/zipcode/dl/kogaki-zip.html"));

			this.Items.Clear();

			foreach (var entry in urls)
			{
				this.Items.Add(new ItemModel { Title = entry.Value, Description = entry.Key.ToString() });
			}

			// TODO: アプリケーションに複数のページが含まれている場合は、次のイベントの
			// 登録によりハードウェアの戻るボタンを処理していることを確認してください:
			// Windows.Phone.UI.Input.HardwareButtons.BackPressed イベント。
			// 一部のテンプレートで指定された NavigationHelper を使用している場合は、
			// このイベントが自動的に処理されます。
		}

		public ObservableCollection<ItemModel> Items
		{
			get;
			private set;
		}
	}

	public sealed class ItemModel
	{
		public string Title
		{
			get;
			set;
		}

		public string Description
		{
			get;
			set;
		}
	}
}
