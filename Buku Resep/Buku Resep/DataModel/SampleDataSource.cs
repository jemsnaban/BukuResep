using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.Resources.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using System.Collections.Specialized;

// The data model defined by this file serves as a representative example of a strongly-typed
// model that supports notification when members are added, removed, or modified.  The property
// names chosen coincide with data bindings in the standard item templates.
//
// Applications may use this model as a starting point and build on it, or discard it entirely and
// replace it with something appropriate to their needs.

namespace Buku_Resep.Data
{
    /// <summary>
    /// Base class for <see cref="SampleDataItem"/> and <see cref="SampleDataGroup"/> that
    /// defines properties common to both.
    /// </summary>
    [Windows.Foundation.Metadata.WebHostHidden]
    public abstract class SampleDataCommon : Buku_Resep.Common.BindableBase
    {
        private static Uri _baseUri = new Uri("ms-appx:///");

        public SampleDataCommon(String uniqueId, String title, String subtitle, String imagePath, String description)
        {
            this._uniqueId = uniqueId;
            this._title = title;
            this._subtitle = subtitle;
            this._description = description;
            this._imagePath = imagePath;
        }

        private string _uniqueId = string.Empty;
        public string UniqueId
        {
            get { return this._uniqueId; }
            set { this.SetProperty(ref this._uniqueId, value); }
        }

        private string _title = string.Empty;
        public string Title
        {
            get { return this._title; }
            set { this.SetProperty(ref this._title, value); }
        }

        private string _subtitle = string.Empty;
        public string Subtitle
        {
            get { return this._subtitle; }
            set { this.SetProperty(ref this._subtitle, value); }
        }

        private string _description = string.Empty;
        public string Description
        {
            get { return this._description; }
            set { this.SetProperty(ref this._description, value); }
        }

        private ImageSource _image = null;
        private String _imagePath = null;
        public ImageSource Image
        {
            get
            {
                if (this._image == null && this._imagePath != null)
                {
                    this._image = new BitmapImage(new Uri(SampleDataCommon._baseUri, this._imagePath));
                }
                return this._image;
            }

            set
            {
                this._imagePath = null;
                this.SetProperty(ref this._image, value);
            }
        }

        public void SetImage(String path)
        {
            this._image = null;
            this._imagePath = path;
            this.OnPropertyChanged("Image");
        }

        public override string ToString()
        {
            return this.Title;
        }
    }

    /// <summary>
    /// Generic item data model.
    /// </summary>
    public class SampleDataItem : SampleDataCommon
    {
        public SampleDataItem(String uniqueId, String title, String subtitle, String imagePath, String description, String content, SampleDataGroup group)
            : base(uniqueId, title, subtitle, imagePath, description)
        {
            this._content = content;
            this._group = group;
        }

        private string _content = string.Empty;
        public string Content
        {
            get { return this._content; }
            set { this.SetProperty(ref this._content, value); }
        }

        private SampleDataGroup _group;
        public SampleDataGroup Group
        {
            get { return this._group; }
            set { this.SetProperty(ref this._group, value); }
        }
    }

    /// <summary>
    /// Generic group data model.
    /// </summary>
    public class SampleDataGroup : SampleDataCommon
    {
        public SampleDataGroup(String uniqueId, String title, String subtitle, String imagePath, String description)
            : base(uniqueId, title, subtitle, imagePath, description)
        {
            Items.CollectionChanged += ItemsCollectionChanged;
        }

        private void ItemsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Provides a subset of the full items collection to bind to from a GroupedItemsPage
            // for two reasons: GridView will not virtualize large items collections, and it
            // improves the user experience when browsing through groups with large numbers of
            // items.
            //
            // A maximum of 12 items are displayed because it results in filled grid columns
            // whether there are 1, 2, 3, 4, or 6 rows displayed

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewStartingIndex < 12)
                    {
                        TopItems.Insert(e.NewStartingIndex, Items[e.NewStartingIndex]);
                        if (TopItems.Count > 12)
                        {
                            TopItems.RemoveAt(12);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (e.OldStartingIndex < 12 && e.NewStartingIndex < 12)
                    {
                        TopItems.Move(e.OldStartingIndex, e.NewStartingIndex);
                    }
                    else if (e.OldStartingIndex < 12)
                    {
                        TopItems.RemoveAt(e.OldStartingIndex);
                        TopItems.Add(Items[11]);
                    }
                    else if (e.NewStartingIndex < 12)
                    {
                        TopItems.Insert(e.NewStartingIndex, Items[e.NewStartingIndex]);
                        TopItems.RemoveAt(12);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldStartingIndex < 12)
                    {
                        TopItems.RemoveAt(e.OldStartingIndex);
                        if (Items.Count >= 12)
                        {
                            TopItems.Add(Items[11]);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    if (e.OldStartingIndex < 12)
                    {
                        TopItems[e.OldStartingIndex] = Items[e.OldStartingIndex];
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    TopItems.Clear();
                    while (TopItems.Count < Items.Count && TopItems.Count < 12)
                    {
                        TopItems.Add(Items[TopItems.Count]);
                    }
                    break;
            }
        }

        private ObservableCollection<SampleDataItem> _items = new ObservableCollection<SampleDataItem>();
        public ObservableCollection<SampleDataItem> Items
        {
            get { return this._items; }
        }

        private ObservableCollection<SampleDataItem> _topItem = new ObservableCollection<SampleDataItem>();
        public ObservableCollection<SampleDataItem> TopItems
        {
            get { return this._topItem; }
        }
    }

    /// <summary>
    /// Creates a collection of groups and items with hard-coded content.
    /// 
    /// SampleDataSource initializes with placeholder data rather than live production
    /// data so that sample data is provided at both design-time and run-time.
    /// </summary>
    public sealed class SampleDataSource
    {
        private static SampleDataSource _sampleDataSource = new SampleDataSource();

        private ObservableCollection<SampleDataGroup> _allGroups = new ObservableCollection<SampleDataGroup>();
        public ObservableCollection<SampleDataGroup> AllGroups
        {
            get { return this._allGroups; }
        }

        public static IEnumerable<SampleDataGroup> GetGroups(string uniqueId)
        {
            try
            {
                if (!uniqueId.Equals("AllGroups")) throw new ArgumentException("Only 'AllGroups' is supported as a collection of groups");
            }
            catch (Exception) { }

            return _sampleDataSource.AllGroups;
        }

        public static SampleDataGroup GetGroup(string uniqueId)
        {
            // Simple linear search is acceptable for small data sets
            var matches = _sampleDataSource.AllGroups.Where((group) => group.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        public static SampleDataItem GetItem(string uniqueId)
        {
            // Simple linear search is acceptable for small data sets
            var matches = _sampleDataSource.AllGroups.SelectMany(group => group.Items).Where((item) => item.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        public SampleDataSource()
        {

            /*String ITEM_CONTENT = String.Format("Item Content: {0}\n\n{0}\n\n{0}\n\n{0}\n\n{0}\n\n{0}\n\n{0}",
                        "Curabitur class aliquam vestibulum nam curae maecenas sed integer cras phasellus suspendisse quisque donec dis praesent accumsan bibendum pellentesque condimentum adipiscing etiam consequat vivamus dictumst aliquam duis convallis scelerisque est parturient ullamcorper aliquet fusce suspendisse nunc hac eleifend amet blandit facilisi condimentum commodo scelerisque faucibus aenean ullamcorper ante mauris dignissim consectetuer nullam lorem vestibulum habitant conubia elementum pellentesque morbi facilisis arcu sollicitudin diam cubilia aptent vestibulum auctor eget dapibus pellentesque inceptos leo egestas interdum nulla consectetuer suspendisse adipiscing pellentesque proin lobortis sollicitudin augue elit mus congue fermentum parturient fringilla euismod feugiat");*/

            var group1 = new SampleDataGroup("Group-1",
                    "Resep Nusantitaara",
                    "",
                    "Assets/Nusantara/nusantara.jpg",
                    "Indonesia merupakan negara yang kaya akan budaya dan kulinernya, maka tidak heran jika negara indonesia banyak dikenal karena kekhasan masakanya. Disetiap daerah memiliki ciri khas masakan tersendiri, resepnya pun satu sama lain mempunyai perbedaan");
            group1.Items.Add(new SampleDataItem("Group-1-Item-1",
                    "Gulai Kambing",
                    "",
                    "Assets/Nusantara/gulai kambing.jpg",
                    "Gulai menjadi primadona kuliner khas masakan Nusantara karena diolah dengan kombinasi bumbu rempah yang pas. Sehingga membuat masakan ini menjadi menu makanan yang lezat saat disantap dalam kondisi apapun.",
                    items.item1_g1, group1));
            group1.Items.Add(new SampleDataItem("Group-1-Item-2",
                    "Otak-otak Jamur",
                    "Otak-otak Jamur Tiram Goreng",
                    "Assets/Nusantara/otak-otak jamur.jpg",
                    "Bukan hanya Menu Otak-otak Bakar saja yang lezat dan nikmat untuk disantap, Otak-otak Jamur pun menjadi menu pilihan yang nikmat dan lezat untuk menu makan siang atau makan malam.",
                    items.item2_g1,
                    group1));
            group1.Items.Add(new SampleDataItem("Group-1-Item-3",
                    "Orak Arik",
                    "Orak Arik Tempe Enak",
                    "Assets/Nusantara/orakarik.jpg",
                    "Masakan orak arik tempe yang mudah kita jumpai di warung makan dapat dikreasi sendiri di rumah, dengan kreasi masakan Anda sendiri akan menghasilkan masakan yang istimewa ketimbang beli sayur di warung makan. :D",
                    items.item3_g1,
                    group1));
            group1.Items.Add(new SampleDataItem("Group-1-Item-4",
                    "Pepes Ikan",
                    "Pepes Ikan Kakap Jakarta",
                    "Assets/Nusantara/pepesikan.jpg",
                    "Pepes merupakan salah satu cara untuk mengolah bahan makanan yang biasanya diperuntukkan untuk mengolah ikan dengan bantuan daun pisang sebagai pembungkus ikan dan bumbu yang dibalut didalamnya.",
                    items.item4_g1,
                    group1));
            group1.Items.Add(new SampleDataItem("Group-1-Item-5",
                    "Asinan",
                    "Asinan Jakarta",
                    "Assets/Nusantara/asinanjakarta.jpg",
                    "Makanan asinan banyak dijumpai di warung-warung tetapi akan lebih enak jika anda membuat asinan dengan kreasi sendiri",
                    items.item5_g1,
                    group1));
            this.AllGroups.Add(group1);

            var group2 = new SampleDataGroup("Group-2",
                    "Resep Daerah",
                    "Makanan Khas Daerah",
                    "Assets/Daerah/daerah.jpg",
                    "Makanan khas daerah indonesia tidak hilang seiring perkembangan masakan-masakan luar negeri. Makanan daerah dikenal khas pada pemanfaatan rempah-rempah yang membuat makanan semakin lezat.");
            group2.Items.Add(new SampleDataItem("Group-2-Item-1",
                    "Soto Betawi",
                    "Soto Jeroan Betawi",
                    "Assets/Daerah/sotobetawi.jpg",
                    "Soto Betawi merupakan jenis soto yang menggunakan santan untuk bahan kuah sotonya. Persoalan rasa jangan diragukan lagi, Soto Betawi ini memiliki rasa yang gurih dengan kuah santan yang segar dan lebih sedap lagi bila ditambah dengan sambal.",
                    items.item1_g2,
                    group2));
            group2.Items.Add(new SampleDataItem("Group-2-Item-2",
                    "Dendeng Balado",
                    "Dendeng Balado Khas Padang",
                    "Assets/Daerah/dendengbalado.jpg",
                    "Seperti kebanyakan masakan khas daerah lainnya, masakan dari daerah Padang ini memiliki ciri khas masakan yang unik dengan citarasa balado yang nikmat khas daerah Padang",
                   items.item2_g2,
                    group2));
            group2.Items.Add(new SampleDataItem("Group-2-Item-3",
                    "Lumpia Goreng",
                    "Lumpia Goreng Khas Semarang",
                    "Assets/Daerah/lumpiasemarang.jpg",
                    "Lumpia goreng merupakan cemilan ringan yang dibuat dari sayur-sayuran dan daging sebagai isinya, makanan ini sangat cocok disajikan saat masih hangat dan begitu nikmat jika dinikmati bersama keluarga.",
                    items.item3_g2,
                    group2));
            group2.Items.Add(new SampleDataItem("Group-2-Item-4",
                   "Mie Goreng",
                   "Mie Goreng Magelangan",
                   "Assets/Daerah/Mie goreng.jpg",
                   "Mie Goreng Jawa atau yang paling terkenal adalah mie goreng magelangan memang lebih enak jika disantap saat malam hari dan dipadukan dengan kerupuk sebagai pelengkap.",
                   items.item4_g2,
                   group2));
            group2.Items.Add(new SampleDataItem("Group-2-Item-5",
                   "Ati Balado",
                   "Ati Ampela Bumbu Balado",
                   "Assets/Daerah/ati ampela.jpg",
                   "Ati ampela yang kaya nutrisi menjadi makanan yang tepat untuk dinikmati bersama keluarga tercinta",
                   items.item5_g2,
                   group2));
            this.AllGroups.Add(group2);

            var group3 = new SampleDataGroup("Group-3",
                    "Resep Asia",
                    "Makanan Khas Asia",
                    "Assets/Asia/asia.jpg",
                    "");
            group3.Items.Add(new SampleDataItem("Group-3-Item-1",
                    "Bakso Loa Loha",
                    "Bakso Special",
                    "Assets/Asia/baksoloa.jpg",
                    "Bakso Loha merupakan makanan khas Asia, sering dimakan sebagai makanan 'pencuci mulut'",
                    items.item1_g3,
                    group3));
            group3.Items.Add(new SampleDataItem("Group-3-Item-2",
                    "Bistik",
                    "Bistik Daging Sapi Special",
                    "Assets/Asia/bistik.jpg",
                    "Berbagai olahan daging biasanya sering menjadi menu unggulan dari restoran-restoran terkemuka, diantaranya adalah Bistik Daging Sapi Spesial",
                    items.item2_g3,
                    group3));
            group3.Items.Add(new SampleDataItem("Group-3-Item-3",
                    "Teriyaki Tofu",
                    "Teriyaki Tofu khas Jepang",
                    "Assets/Asia/teriyakitofu.jpg",
                    "Makanan khas olahan Jepang ini menjadi salah satu menu favorit di beberapa restoran terkemuka di Indonesia.",
                    items.item3_g3,
                    group3));
            group3.Items.Add(new SampleDataItem("Group-3-Item-4",
                    "Cumi Bakar",
                    "Cumi Bakar Khas Jepang",
                    "Assets/Asia/cumi.jpg",
                    "Memasak masakan Cumi bakar ala Jepang ternyata tidak sulit, bahan-bahanya pun mudah ditemukan di sekitar kita.",
                    items.item4_g3,
                    group3));
            group3.Items.Add(new SampleDataItem("Group-3-Item-5",
                    "Mie KungPau",
                    "Mie Goreng Khas China",
                    "Assets/Asia/miegorengcina.jpg",
                    "Daratan Asia terkenal dengan aneka olahan mie, salah satunya adalah mie goreng olahan China yang sangat enak dan lezat",
                    items.item5_g3,
                    group3));
            this.AllGroups.Add(group3);

            var group4 = new SampleDataGroup("Group-4",
                    "Eropa",
                    "Makanan Khas Eropa",
                    "Assets/Eropa/eropa.jpg",
                    "");
            group4.Items.Add(new SampleDataItem("Group-4-Item-1",
                    "Pancake ",
                    "Pancake Swedia",
                    "Assets/Eropa/Pancake.jpg",
                    "Jika anda tertarik ingin mencoba Membuat Pancake Dari Swedia, resep ini bisa anda coba sebagai panduan anda untuk memulai membuat pancake enak dan lezat.",
                    items.item1_g4,
                    group4));
            group4.Items.Add(new SampleDataItem("Group-4-Item-2",
                    "Borju paprika's ",
                    "Kambing Bumbu Paprika",
                    "Assets/Eropa/borjupaprikas.jpg",
                    "Bagi penggemar kambing mungkin ini adalah salah satu resep yang patut anda coba. Rasanya sangat lezat ditambah aroma paprika yang khas.",
                    items.item2_g4,
                    group4));
            group4.Items.Add(new SampleDataItem("Group-4-Item-3",
                    "Cheese Cannelloni",
                    "Cheese Cannelloni with Spinach ",
                    "Assets/Eropa/cheese.jpg",
                    "Pencinta pasta saatnya menikmati hidangan unik yang satu ini, Cheese Cannelloni with Spinach! Cita rasa bayam yang manis dan segar berpadu dengan daging giling, pasta canneloni, black pepper cheese dan bumbu pilihan lainnya.",
                    items.item3_g4,
                    group4));
            group4.Items.Add(new SampleDataItem("Group-4-Item-4",
                    "French Fries",
                    "French Fries with Pepperoni Sauce Dip",
                    "Assets/Eropa/frencfries.jpg",
                    "Kentang goreng yang dikukus dan dibumbui sebelum digoreng, menghasilkan kentang yang nikmat dan lezat",
                    items.item4_g4,
                    group4));
            group4.Items.Add(new SampleDataItem("Group-4-Item-5",
                    "Kopasztas Kocka ",
                    "Kol Masak Pasta",
                    "Assets/Eropa/kopasztasKocka.jpg",
                    "Bagi penggemar makanan vegetarian, coba deh resep yang satu ini :)",
                    items.item5_g4,
                    group4));
            group4.Items.Add(new SampleDataItem("Group-4-Item-6",
                    "Makaroni Keju Daging ",
                    "Makaroni Daging Sapi",
                    "Assets/Eropa/makaroni.jpg",
                    "Bagi yang menyukai pasta, snack ini sangatlah pas untuk dimakan saat sore hari ataupun saat berpergian. Daging sapi dan susu memiliki kandungan gizi yang tinggi. Taburan keju membuat hidangan ini semakin lezat.",
                    items.item6_g4,
                    group4));
            this.AllGroups.Add(group4);

            var group5 = new SampleDataGroup("Group-5",
                    "Seafood",
                    "",
                    "Assets/Seafood/seafood.jpg",
                    "");
            group5.Items.Add(new SampleDataItem("Group-5-Item-1",
                    "Balado Cumi",
                    "Balado Cumi Cabai Hijau",
                    "Assets/Seafood/cumi.jpg",
                    "Pedasnya cabai hijau yang diguakan untuk balado cumi ini tak sepedas bila menggunakan cabai merah. Maka untuk Anda yang tak begitu suka dengan cita rasa pedas yang menyengat sajian balado cumi cabai hijau ini akan terasa pas dilidah.",
                    items.item1_g5,
                    group5));
            group5.Items.Add(new SampleDataItem("Group-5-Item-2",
                    "Ikan Acar Tomat",
                    "Ikan Mujair ditabur Acar Tomat",
                    "Assets/Seafood/ikantomat.jpg",
                    "Ikan Mujair disajikan dengan cara yang berbeda yaitu dengan disiram dengan acar tomat segar.",
                    items.item2_g5,
                    group5));
            group5.Items.Add(new SampleDataItem("Group-5-Item-3",
                    "Cumi Bakar",
                    "",
                    "Assets/Seafood/cumibakar.jpg",
                    "Selain dibuat balado, cumi juga bisa dihidangkan dengan cara dibakar.",
                    items.item3_g5,
                    group5));
            group5.Items.Add(new SampleDataItem("Group-5-Item-4",
                    "Udang Mayonnaise",
                    "Udang Kupas Saus Mayonnaise",
                    "Assets/Seafood/udang.jpg",
                    "Gurihnya udang kupas yang digoreng kering, berpadu dengan citarasa manis sedikit asam dari saus mayonaise, akan memuaskan lidah",
                    items.item4_g5,
                    group5));
            this.AllGroups.Add(group5);

            var group6 = new SampleDataGroup("Group-6",
                    "Aneka Kue",
                    "Aneka Olahan Kue Indonesia",
                    "Assets/Kue/kue.jpg",
                    "");
            group6.Items.Add(new SampleDataItem("Group-6-Item-1",
                    "Roti Bolu Gulung",
                    "Roti Bolu Gulung O'reo",
                    "Assets/Kue/rotibolu.jpg",
                    "Roti Bolu gulung ini memanfaatkan biskuit O'reo sebagai bahan olesan sehingga terlihat menarik dan terasa semakin enak.",
                    items.item1_g6,
                    group6));
            group6.Items.Add(new SampleDataItem("Group-6-Item-2",
                    "Bika Ambon",
                    "Bika Ambon Istimewa Khas Medan",
                    "Assets/Kue/bikaambon.jpg",
                    "Bika ambon termasuk jenis kue basah, memiliki rasa yang enak, manis dan lembut membuat kue ini banyak disukai. Selain itu kue ini juga dapat bertahan cukup lama.",
                    items.item2_g6,
                    group6));
            group6.Items.Add(new SampleDataItem("Group-6-Item-3",
                    "Kue Pukis",
                    "Kue Pukis Keju",
                    "Assets/Kue/pukis.jpg",
                    "Pukis kentang yang ditabur keju yang meleleh di atasnya menghasilkan rasa yang sangat nikmat di mulut.",
                    items.item3_g6,
                    group6));
            group6.Items.Add(new SampleDataItem("Group-6-Item-4",
                    "Wingko Babat",
                    "Wingko Babat Semarang",
                    "Assets/Kue/wingko.jpg",
                    "Kue yang dipanggang dan berbahan tepung ketan serta kelapa parut kasar ini memang sangat nikmat. Apalagi saat dinikmati dengan secangkir kopi panas sajian ini terasa istimewa.",
                    items.item4_g6,
                    group6));
            group6.Items.Add(new SampleDataItem("Group-6-Item-5",
                    "Kue Wajik",
                    "Kue Wajik Pandan",
                    "Assets/Kue/wajik.jpg",
                    "Salah satu hidangan favorit di rumah bersama keluarga",
                    items.item5_g6,
                    group6));
            group6.Items.Add(new SampleDataItem("Group-6-Item-6",
                    "Kue Putu Mayang",
                    "Putu Mayang Enak Lezat",
                    "Assets/Kue/putu.jpg",
                    "Warna hijau yang cerah membuat menu ini semakin unik. Dan taburan kelapa yang diparut membuat putu mayang lebih enak saat dinikmati.",
                    items.item6_g6,
                    group6));
            this.AllGroups.Add(group6);
        }
    }
}
