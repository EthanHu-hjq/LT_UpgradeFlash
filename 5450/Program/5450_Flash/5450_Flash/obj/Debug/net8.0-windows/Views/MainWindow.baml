   M S B A M L   `   `   `  ����E  A5450_Flash, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null)clr-namespace:_5450_Flash_5450_Flash  5   	       _5450_Flash.MainWindow   5   	   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   +localclr-namespace:_5450_Flash     5   	     � Title$  
MainWindow��5   	   $	��450��6   $	��800��6)   .��5	      �  � RowDefinitions 5
   
   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6    � ColumnDefinitions 5   
   �� 5      $��* �6   �� 5      $��* �6   .+�5   
   �� $��第一个数字:��6   $��0��6!   $��0��6.   �� 5   
   $��0��6Z   $��1��6g   ��6   �� *calModel.firstNum+ �� UpdateSourceTrigger$ PropertyChanged=�6g   �� 5   
   $��第二个数字:��6   $��1��6!   $��0��6.   �� 5   
   $��1��6[   $��1��6h   ��6   �� *calModel.secondNum+$ PropertyChanged=�6h   �� 5   
   $��加法��6   $��2��6=   $��0��6J   ��6   �� *
AddCommand+6J   �� 5   
   $��减法��6   $��2��6=   $��1��6J   ��6   �� *
SubCommand+6J   �� 5   
   $��结果:��6   $��3��6   $��0��6+   �� 5   
   $��3��65   $��1��6B   ��6   �� *calModel.Result+6B   5      5      