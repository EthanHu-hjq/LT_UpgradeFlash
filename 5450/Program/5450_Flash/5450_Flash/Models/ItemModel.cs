using _5450_Flash.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _5450_Flash.Models
{
    public class ItemModel:NotifyBase
    {
		private string _displayName;

		public string DisplayName
		{
			get => _displayName;
			set
			{
				if (_displayName != value)
				{
					_displayName = value;
					RaisePropertyChanged();
				}
			}
		}

		private int _value;

		public int Value
		{
			get => _value;
			set
			{
				if(_value != value)
                {
                    _value = value;
                    RaisePropertyChanged();
                }
            }
		}
	}
}
