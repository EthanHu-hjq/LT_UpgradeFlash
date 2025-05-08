using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using _5450_Flash.Base;
using _5450_Flash.Models;

namespace _5450_Flash.ViewModels
{
    public class MainViewModel
    {
        public CalModel calModel { get; set; }

        public ICommand AddCommand { get; set; }
        public ICommand SubCommand { get; set; }
        public MainViewModel() { 
            calModel = new CalModel();
            AddCommand = new RelayCommand(Add);
            SubCommand = new RelayCommand(Sub);
        }

        private void Sub(object obj)
        {
            calModel.Result = calModel.Sub();
        }

        private void Add(object obj)
        {
            calModel.Result = calModel.Add();
        }
        
    }
}
