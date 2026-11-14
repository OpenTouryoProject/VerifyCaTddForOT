<%@ Page Language="C#" MasterPageFile="~/Aspx/Common/Master/footerScreen.master" AutoEventWireup="true" Inherits="WebForms_Sample.Aspx.OrdTf.OrdTf" Codebehind="OrdTf.aspx.cs" %>
<%@ Register Assembly="OpenTouryo.CustomControl" Namespace="Touryo.Infrastructure.CustomControl" TagPrefix="cc1" %>

<asp:Content ID="cphHeaderScripts" ContentPlaceHolderID="cphHeaderScripts" Runat="Server">
    <!-- Head 部の ContentPlaceHolder -->
</asp:Content>

<%-- コンテンツ部（SC-ORD-01）。サーバ コントロールの id は ClientIDMode="Static" で、画面定義の id のまま出す。
     接頭辞（btn・ddl・txt・gvw など）で始まる id は、フレームワークがイベントを自動結線する。
     入力欄・一覧には接頭辞を付けない（変更・行のイベントは使わない）。ページ移動の 4 つのボタンだけ btn を付ける。 --%>
<asp:Content ID="ContentPlaceHolder_A" ContentPlaceHolderID="ContentPlaceHolder_A" Runat="Server">
    <%-- 共通メッセージ領域（エラーは text-danger、情報は text-info） --%>
    <div>
        <asp:Label ID="messageArea" ClientIDMode="Static" runat="server" Text=""></asp:Label>
    </div>

    <%-- 検索条件（SC-ORD-01-I01〜I05） --%>
    <div class="row">
        <div class="form-group col-6">
            <label for="CustomerID">顧客</label>
            <asp:DropDownList ID="CustomerID" ClientIDMode="Static" runat="server" CssClass="form-control"></asp:DropDownList>
        </div>
        <div class="form-group col-6">
            <label for="EmployeeID">担当者</label>
            <asp:DropDownList ID="EmployeeID" ClientIDMode="Static" runat="server" CssClass="form-control"></asp:DropDownList>
        </div>
    </div>
    <div class="row">
        <div class="form-group col-4">
            <label for="OrderDateFrom">受注日（開始）</label>
            <asp:TextBox ID="OrderDateFrom" ClientIDMode="Static" runat="server" CssClass="form-control" placeholder="yyyy/MM/dd"></asp:TextBox>
            <asp:Label ID="OrderDateFromError" ClientIDMode="Static" runat="server" CssClass="text-danger" Text=""></asp:Label>
        </div>
        <div class="form-group col-4">
            <label for="OrderDateTo">受注日（終了）</label>
            <asp:TextBox ID="OrderDateTo" ClientIDMode="Static" runat="server" CssClass="form-control" placeholder="yyyy/MM/dd"></asp:TextBox>
            <asp:Label ID="OrderDateToError" ClientIDMode="Static" runat="server" CssClass="text-danger" Text=""></asp:Label>
        </div>
        <div class="form-group col-4">
            <label for="ShippedStatus">出荷状況</label>
            <asp:DropDownList ID="ShippedStatus" ClientIDMode="Static" runat="server" CssClass="form-control"></asp:DropDownList>
        </div>
    </div>

    <%-- 検索結果（SC-ORD-01-I06〜I08） --%>
    <div>
        <asp:Label ID="totalCount" ClientIDMode="Static" runat="server" Text=""></asp:Label>
    </div>
    <%-- 一覧は GridView。見出しは thead、データ行は tbody に出す（コードビハインドで HeaderRow.TableSection を指定）。
         0 件のとき行を出さない（EmptyData を指定しない）。組み込みのページングは使わず、毎回バインドし直す。 --%>
    <asp:GridView ID="orderList" ClientIDMode="Static" runat="server" AutoGenerateColumns="false"
        UseAccessibleHeader="true" EnableViewState="false" AllowPaging="false" AllowSorting="false"
        CssClass="table table-sm table-bordered" GridLines="None" BorderWidth="0">
        <Columns>
            <asp:BoundField DataField="OrderID" HeaderText="受注 ID" />
            <asp:BoundField DataField="CustomerName" HeaderText="顧客名" />
            <asp:BoundField DataField="EmployeeName" HeaderText="担当者名" />
            <asp:BoundField DataField="OrderDate" HeaderText="受注日" />
            <asp:BoundField DataField="RequiredDate" HeaderText="要求納期" />
            <asp:BoundField DataField="ShippedDate" HeaderText="出荷日" />
            <asp:BoundField DataField="ShipperName" HeaderText="配送業者" />
            <asp:BoundField DataField="TotalAmount" HeaderText="合計金額" />
            <asp:BoundField DataField="Status" HeaderText="状況" />
        </Columns>
    </asp:GridView>

    <%-- ページ移動（SC-ORD-01-B05。先頭・前・次・末尾の 4 つ。該当方向にページが存在するときだけ活性） --%>
    <div>
        <asp:Label ID="pageInfo" ClientIDMode="Static" runat="server" Text=""></asp:Label>
        <cc1:WebCustomButton ID="btnPageFirst" ClientIDMode="Static" runat="server" Text="先頭" CssClass="btn btn-outline-primary btn-sm" />
        <cc1:WebCustomButton ID="btnPagePrev" ClientIDMode="Static" runat="server" Text="前へ" CssClass="btn btn-outline-primary btn-sm" />
        <cc1:WebCustomButton ID="btnPageNext" ClientIDMode="Static" runat="server" Text="次へ" CssClass="btn btn-outline-primary btn-sm" />
        <cc1:WebCustomButton ID="btnPageLast" ClientIDMode="Static" runat="server" Text="末尾" CssClass="btn btn-outline-primary btn-sm" />
    </div>
</asp:Content>

<asp:Content ID="cphFooterScripts" ContentPlaceHolderID="cphFooterScripts" Runat="Server">
    <!-- Footer 部の ContentPlaceHolder -->
</asp:Content>
