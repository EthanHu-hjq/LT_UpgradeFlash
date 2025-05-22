using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using _5450_Flash.Base;
using _5450_Flash.Models;
using Microsoft.Win32;

namespace _5450_Flash.ViewModels
{
    public class BurnViewModel
    {
        #region 初始化ComboBox
        ///// <summary>
        ///// 选中项（直接暴露模型对象）
        ///// </summary>
        //private ItemModel _selectedItem;

        ////数据源集合（无需通知）
        //public ObservableCollection<ItemModel> Items { get; } = new()
        //{
        //    new ItemModel{ DisplayName = "Type 0:SM Bus Hub(SM bus cable)", Value = 0 },
        //    new ItemModel{ DisplayName = "Type 1:USB BillBoard(C(A) to A(B) cable", Value = 1 },
        //    new ItemModel{ DisplayName = "Type 2:TypeC CC(CC cable)", Value = 2 },
        //    new ItemModel{ DisplayName = "Type 3:Sm Bus HID to I2C(no cable)", Value = 3 },
        //    new ItemModel{ DisplayName = "Type 4:SM Bus EC(SM bus cable)", Value = 4 },
        //    new ItemModel{ DisplayName = "Type 5:I2C Bus PCH(no cable)", Value = 5 },
        //    new ItemModel{ DisplayName = "Type 6:SM Bus PCH(no cable)", Value = 6 }
        //};

        ///// <summary>
        ///// 选中项（直接暴露模型对象）
        ///// </summary>
        //public ItemModel SelectedItem
        //{
        //    get => _selectedItem;
        //    set => _selectedItem = value;//当ItemModel自身属性变化时自动触发通知
        //}

        #endregion

        public BurnModel BurnModel { get; set; }//模型对象

        /// <summary>
        /// BurnViewModel构造函数
        /// </summary>
        public BurnViewModel()
        { 
            BurnModel = new BurnModel();
            BurnModel.OpenCmd = new RelayCommand(Open);
            BurnModel.CloseCmd = new Base.RelayCommand(Close);
            BurnModel.BrowseCmd = new Base.RelayCommand(Browse);
            BurnModel.ReadVerCmd = new Base.RelayCommand(ReadVer);
            BurnModel.EraseCmd = new Base.RelayCommand(Erase);
            BurnModel.LoadBurnFileCmd = new RelayCommand(LoadBurnFile);
            BurnModel.SetBusTypeCmd = new RelayCommand(SelectBusType);
            BurnModel.UpdateCmd = new RelayCommand(UpdateExe);
            BurnModel.SetDev1 = new RelayCommand(SetDev1);
            BurnModel.ShowExeCmd = new RelayCommand(Show);
            BurnModel.Items = new ObservableCollection<ItemModel>
            {
                new ItemModel{ Name = "Type 0:SM Bus Hub(SM bus cable)", Id = 0 },
                new ItemModel{ Name = "Type 1:USB BillBoard(C(A) to A(B) cable", Id = 1 },
                new ItemModel{ Name = "Type 2:TypeC CC(CC cable)", Id = 2 },
                new ItemModel{ Name = "Type 3:Sm Bus HID to I2C(no cable)", Id = 3 },
                new ItemModel{ Name = "Type 4:SM Bus EC(SM bus cable)", Id = 4 },
                new ItemModel{ Name = "Type 5:I2C Bus PCH(no cable)", Id = 5 },
                new ItemModel{ Name = "Type 6:SM Bus PCH(no cable)", Id = 6 }
            };
        }

        private void Show(object obj)
        {
            BurnModel.ShowExe();
        }

        private void SetDev1(object obj)
        {
            BurnModel.SetDev1Btn();
        }

        private void UpdateExe(object obj)
        {
            BurnModel.Update();
        }

        private void SelectBusType(object obj)
        {
            BurnModel.SelectBusType();
        }

        private void LoadBurnFile(object obj)
        {
            BurnModel.FW1_Path();
        }

        private void Erase(object obj)
        {
            BurnModel.Erase();
        }

        private void ReadVer(object obj)
        {
            BurnModel.ReadVersion();
        }

        #region 方法
        private void Browse(object obj)
        {
            BurnModel.Browse();
        }

        private void Close(object obj)
        {
            BurnModel.CloseExe();
        }

        private void Open(object obj)
        {
            BurnModel.OpenExe();
        }
        #endregion
    }
}
