﻿<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" CodeFile="LM501027.aspx.cs" Inherits="Pages_LM501027" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%"
        TypeName="LumTomofunCustomization.Graph.LUMPaypalJP_DocomoPaymentProcess"
        PrimaryView="PaymentTransactions">
        <CallbackCommands>
        </CallbackCommands>
    </px:PXDataSource>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
    <px:PXGrid SyncPosition="True" ID="grid" runat="server" DataSourceID="ds" Width="100%" Height="150px" SkinID="Primary" AllowAutoHide="false">
        <Levels>
            <px:PXGridLevel DataMember="PaymentTransactions">
                <Columns>
                    <px:PXGridColumn DataField="Selected" Type="CheckBox" AllowCheckAll="true"></px:PXGridColumn>
                    <px:PXGridColumn DataField="IsProcessed" Type="CheckBox"></px:PXGridColumn>
                    <px:PXGridColumn DataField="Marketplace"></px:PXGridColumn>
                    <px:PXGridColumn DataField="TransactionDate"></px:PXGridColumn>
                    <px:PXGridColumn DataField="TransactionType"></px:PXGridColumn>
                    <px:PXGridColumn DataField="OrderID" />
                    <px:PXGridColumn DataField="Gross" />
                    <px:PXGridColumn DataField="Fee" />
                    <px:PXGridColumn DataField="Net" />
                    <px:PXGridColumn DataField="Description" />
                    <px:PXGridColumn DataField="OrigOrderID" />
                    <px:PXGridColumn DataField="Currency" />
                    <px:PXGridColumn DataField="ErrorMessage" />
                    <px:PXGridColumn DataField="CreatedDateTime" Width="130" DisplayFormat="g"></px:PXGridColumn>
                    <px:PXGridColumn DataField="CreatedByID" Width="130" DisplayFormat="g"></px:PXGridColumn>
                </Columns>
            </px:PXGridLevel>
        </Levels>
        <AutoSize Container="Window" Enabled="True" MinHeight="150" />
        <ActionBar>
        </ActionBar>
        <Mode AllowUpdate="True" AllowUpload="True" AllowDelete="True" AllowAddNew="True" />
    </px:PXGrid>

</asp:Content>
