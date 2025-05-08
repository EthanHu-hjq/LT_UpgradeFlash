using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _5450_Flash.Base;

namespace _5450_Flash.Models
{
    public class CalModel:NotifyBase
    {
        public double firstNum { get; set; }

        public double secondNum { get; set; }

        private double _result;

		public double Result
		{
			get { return _result; }
			set { 
				_result = value;
				RaisePropertyChanged(nameof(Result));
			}
		}

		public double Add()
		{
			return firstNum + secondNum;
		}	

		public double Sub()
		{
			return firstNum - secondNum;
		}

	}
}
