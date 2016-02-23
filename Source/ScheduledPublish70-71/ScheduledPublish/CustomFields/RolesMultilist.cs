using System.Collections.Generic;
using System.Web.UI;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Resources;
using Sitecore.Security.Accounts;
using Sitecore.Shell.Applications.ContentEditor;

namespace ScheduledPublish.CustomFields
{
    public class RolesMultilist : MultilistEx
    {
        private IRolesField _RolesField;
        private IRolesField RolesField
        {
            get
            {
                if (_RolesField == null)
                {
                    _RolesField = CreateNewRolesField();
                }

                return _RolesField;
            }
        }

        public RolesMultilist()
        {
        }

        protected override void DoRender(HtmlTextWriter output)
        {
            Assert.ArgumentNotNull(output, "output");

            SetIDProperty();
            string disabledAttribute = string.Empty;

            if (ReadOnly)
            {
                disabledAttribute = " disabled=\"disabled\"";
            }

            output.Write("<input id=\"{0}_Value\" type=\"hidden\" value=\"{1}\" />", ID, StringUtil.EscapeQuote(Value));
            output.Write("<table{0}>", GetControlAttributes());
            output.Write("<tr>");
            output.Write("<td class=\"scContentControlMultilistCaption\" width=\"50%\">{0}</td>", GetAllLabel());
            output.Write("<td width=\"20\">{0}</td>", Images.GetSpacer(20, 1));
            output.Write("<td class=\"scContentControlMultilistCaption\" width=\"50%\">{0}</td>", GetSelectedLabel());
            output.Write("<td width=\"20\">{0}</td>", Images.GetSpacer(20, 1));
            output.Write("</tr>");
            output.Write("<tr>");
            output.Write("<td valign=\"top\" height=\"100%\">");
            output.Write("<select id=\"{0}_unselected\" class=\"scContentControlMultilistBox\" multiple=\"multiple\"{1} size=\"10\" ondblclick=\"javascript:scContent.multilistMoveRight('{2}')\" onchange=\"javascript:document.getElementById('{3}_all_help').innerHTML=this.selectedIndex>=0?this.options[this.selectedIndex].innerHTML:''\">", ID, disabledAttribute, ID, ID);

            IEnumerable<Role> unselectedRoles = GetUnselectedRoles();
            foreach (Role unselectedRole in unselectedRoles)
            {
                output.Write("<option value=\"{0}\">{1}</option>", GetProviderRoleKey(unselectedRole), unselectedRole.Name);
            }

            output.Write("</select>");
            output.Write("</td>");
            output.Write("<td valign=\"top\">");
            RenderButton(output, "Core/16x16/arrow_blue_right.png", string.Format("javascript:scContent.multilistMoveRight('{0}')", ID));
            output.Write("<br />");
            RenderButton(output, "Core/16x16/arrow_blue_left.png", string.Format("javascript:scContent.multilistMoveLeft('{0}')", ID));
            output.Write("</td>");
            output.Write("<td valign=\"top\" height=\"100%\">");
            output.Write("<select id=\"{0}_selected\" class=\"scContentControlMultilistBox\" multiple=\"multiple\"{1} size=\"10\" ondblclick=\"javascript:scContent.multilistMoveLeft('{2}')\" onchange=\"javascript:document.getElementById('{3}_selected_help').innerHTML=this.selectedIndex>=0?this.options[this.selectedIndex].innerHTML:''\">", ID, disabledAttribute, ID, ID);

            IEnumerable<Role> selectedRoles = GetSelectedRoles();
            foreach (Role selectedRole in selectedRoles)
            {
                output.Write("<option value=\"{0}\">{1}</option>", GetProviderRoleKey(selectedRole), selectedRole.Name);
            }

            output.Write("</select>");
            output.Write("</td>");
            output.Write("<td valign=\"top\">");
            RenderButton(output, "Core/16x16/arrow_blue_up.png", string.Format("javascript:scContent.multilistMoveUp('{0}')", ID));
            output.Write("<br />");
            RenderButton(output, "Core/16x16/arrow_blue_down.png", string.Format("javascript:scContent.multilistMoveDown('{0}')", ID));
            output.Write("</td>");
            output.Write("</tr>");
            output.Write("<tr>");
            output.Write("<td valign=\"top\">");
            output.Write("<div style=\"border:1px solid #999999;font:8pt tahoma;padding:2px;margin:4px 0px 4px 0px;height:14px\" id=\"{0}_all_help\"></div>", ID);
            output.Write("</td>");
            output.Write("<td></td>");
            output.Write("<td valign=\"top\">");
            output.Write("<div style=\"border:1px solid #999999;font:8pt tahoma;padding:2px;margin:4px 0px 4px 0px;height:14px\" id=\"{0}_selected_help\"></div>", ID);
            output.Write("</td>");
            output.Write("<td></td>");
            output.Write("</tr>");
            output.Write("</table>");
        }

        protected void SetIDProperty()
        {
            ServerProperties["ID"] = ID;
        }

        protected static string GetAllLabel()
        {
            return GetLabel("All");
        }

        protected static string GetSelectedLabel()
        {
            return GetLabel("Selected");
        }

        protected static string GetLabel(string key)
        {
            return Translate.Text(key);
        }

        protected IEnumerable<Role> GetSelectedRoles()
        {
            return RolesField.GetSelectedRoles();
        }

        protected IEnumerable<Role> GetUnselectedRoles()
        {
            return RolesField.GetUnselectedRoles();
        }

        protected string GetProviderRoleKey(Role role)
        {
            return RolesField.GetProviderRoleKey(role);
        }

        // Method "borrowed" from MultilistEx control
        protected void RenderButton(HtmlTextWriter output, string icon, string click)
        {
            Assert.ArgumentNotNull(output, "output");
            Assert.ArgumentNotNull(icon, "icon");
            Assert.ArgumentNotNull(click, "click");
            ImageBuilder builder = new ImageBuilder
            {
                Src = icon,
                Width = 0x10,
                Height = 0x10,
                Margin = "2px"
            };
            if (!ReadOnly)
            {
                builder.OnClick = click;
            }
            output.Write(builder.ToString());
        }

        private IRolesField CreateNewRolesField()
        {
            return CustomFields.RolesField.CreateNewRolesField(Source, Value);
        }
    }
}