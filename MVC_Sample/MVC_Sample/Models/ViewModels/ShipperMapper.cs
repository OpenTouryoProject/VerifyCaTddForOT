//**********************************************************************************
//* サンプル アプリ・モデル
//**********************************************************************************

//**********************************************************************************
//* クラス名        ：ShipperMapper
//* クラス日本語名  ：ViewModel と Shared（B層）の引数・戻り値クラスの変換
//*
//* 作成日時        ：2026/11/13
//* 作成者          ：セットアップ
//* 更新履歴        ：
//*
//*  日時        更新者            内容
//*  ----------  ----------------  -------------------------------------------------
//*  2026/11/13  セットアップ      新規作成（B層を Shared に置いて共用するため）
//**********************************************************************************

using System.Collections.Generic;
using System.Data;

using WSIFType_sample;

namespace MVC_Sample.Models.ViewModels
{
    /// <summary>
    /// ViewModel（ShipperViweModel）と、Shared の B層の引数・戻り値クラスを相互に変換する
    /// </summary>
    public static class ShipperMapper
    {
        /// <summary>ViewModel の内容を引数クラスへ設定する（ShipperID は long? → int）</summary>
        /// <param name="param">引数クラス</param>
        /// <param name="shipper">ViewModel</param>
        public static void ToParameter(TestParameterValue param, ShipperViweModel shipper)
        {
            if (shipper == null) return;
            param.ShipperID = shipper.ShipperID.HasValue ? (int)shipper.ShipperID.Value : 0;
            param.CompanyName = shipper.CompanyName;
            param.Phone = shipper.Phone;
        }

        /// <summary>戻り値クラス（1 件）から ViewModel を作る</summary>
        /// <param name="ret">戻り値クラス</param>
        /// <returns>ViewModel</returns>
        public static ShipperViweModel ToViewModel(TestReturnValue ret)
        {
            return new ShipperViweModel()
            {
                ShipperID = ret.ShipperID,
                CompanyName = ret.CompanyName,
                Phone = ret.Phone
            };
        }

        /// <summary>戻り値の Obj（DataTable / DataSet）から ViewModel の一覧を作る（列名でなく位置で読む）</summary>
        /// <param name="obj">戻り値クラスの Obj</param>
        /// <returns>ViewModel の一覧</returns>
        public static List<ShipperViweModel> ToList(object obj)
        {
            DataTable dt = obj as DataTable;
            if (dt == null && obj is DataSet ds) dt = ds.Tables[0];

            List<ShipperViweModel> list = new List<ShipperViweModel>();
            if (dt == null) return list;

            foreach (DataRow dr in dt.Rows)
            {
                list.Add(new ShipperViweModel()
                {
                    ShipperID = dr[0] == System.DBNull.Value ? (long?)null : System.Convert.ToInt64(dr[0]),
                    CompanyName = dr[1] == System.DBNull.Value ? null : dr[1].ToString(),
                    Phone = dr[2] == System.DBNull.Value ? null : dr[2].ToString()
                });
            }
            return list;
        }
    }
}