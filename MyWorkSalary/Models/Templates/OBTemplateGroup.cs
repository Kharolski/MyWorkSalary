using System.Collections.ObjectModel;

namespace MyWorkSalary.Models.Templates
{
    public class OBTemplateGroup : ObservableCollection<OBRateTemplate>
    {
        public string GroupTitle { get; set; }

        public OBTemplateGroup(string title, IEnumerable<OBRateTemplate> templates) : base(templates)
        {
            GroupTitle = title;
        }
    }

}
