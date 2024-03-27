<%@ Page Language="C#" MasterPageFile="~/MasterPages/TabView.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="LM101003.aspx.cs" Inherits="Page_LM101003" Title="Untitled Page" %>
<%@ MasterType VirtualPath="~/MasterPages/TabView.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" Runat="Server">
	<px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" TypeName="LUMLocalization.Graph.LUMTomofunSetupMaint" PrimaryView="Setup">
		<CallbackCommands>
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" Runat="Server">
	<px:PXTab ID="tab" runat="server" DataSourceID="ds" Height="150px" Style="z-index: 100" Width="100%" AllowAutoHide="false" DataMember="Setup">
		<Items>
			<px:PXTabItem Text="General">
                <Template>
					<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="XM" ControlSize="XM" />
					<px:PXPanel ID="CstPXPanel1" runat="server" Caption="Shipment" RenderStyle="Fieldset">
						<px:PXLayoutRule ID="PXLayoutRule1" runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
						<px:PXNumberEdit runat="server" ID="CstPXNumberEdit1" DataField="DefPalletLength" ></px:PXNumberEdit>
						<px:PXNumberEdit runat="server" ID="CstPXNumberEdit2" DataField="DefPalletWidth" ></px:PXNumberEdit>
						<px:PXNumberEdit runat="server" ID="CstPXNumberEdit3" DataField="DefPalletHeight" ></px:PXNumberEdit>
					</px:PXPanel>
                </Template>
			</px:PXTabItem>
		</Items>
		<AutoSize Container="Window" Enabled="True" MinHeight="200" />
	</px:PXTab>
</asp:Content>