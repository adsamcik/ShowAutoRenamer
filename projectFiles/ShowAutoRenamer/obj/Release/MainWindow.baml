   M S B A M L   `   `   `  ����   ShowAutoRenamer     ShowAutoRenamer.MainWindow   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     -     � Title$  Show renamer��$	��320��$	��650�� � 
ResizeMode$ NoResize=� J� 	AllowDrop .  J� SnapsToDevicePixels .  � WindowStartupLocation$ Manual=���.  � AllowsTransparency .  � WindowStyle$
 None=� � Icon$ Icons/Logo.png��.��� ���	#FFF0F0F0 � RowDefinitions �� $��25 ��� 	 � ColumnDefinitions	 �� $��6* ��� $��8* �.+��� $	��Top=�
�������$��
10,10,10,0q�$��1��$	��270��$��1��$��0q�$��Segoe UI Semibold)�.���� -   
 ��Name$
 FileListBox��$	��260��$	��341��$��0q�
������ ���� .R���  �� Orientation$ 
Horizontal=�.+��� ���� *nameWithExtension+� $��1��}�. 	 �� $��8* ��� $��8* �.+��� -    ��Name$ ToggleRemoveDash��$��Remove -��$
��Left=�$��
10,168,0,0q�$	��Top=�
���   �$��15��$��69��{�.  �� -   $ ToggleRemoveUnderscore��$��Remove _��$
��Left=�$��
10,188,0,0q�$	��Top=�
���   �$��15��$��70��{�.  
��������� -   $ ToggleSmartRename��$��Smart-Rename��$
��Left=�$��	8,168,0,0q�$	��Top=�
���   �$��15��$	��108�� d� 	IsChecked$
 TrueY�$��1��� $��13,208,11,57q�$��Center=�$��Center=�$��2��$	��255��.+��� -    ��Name$ InputShowName��$��25��$	��Top=����	#00E6E6E6$��Bottom=�$��Segoe UI Semibold)����	#FF648CE1$��0,0,0,2q�$��0,0,0,1q� �� SelectionBrush
 �   �
���   �   textBoxStyle#���   $��Stretch=�}�. �� -    ��Name$ showNameOverText��$��	Show name��$��Bottom=�$��24�� �� Padding$ 3,4,0,0q� J� IsHitTestVisible .  ���	#FFA0A0A0$��Segoe UI Semibold)�$
��Left=�$��75��$��	-1,0,0,-1q��� -   $��Select��$��Center=�$	��115��$��30��$��0q����	#FF648CE1
�������$��18(�$��Segoe UI Semibold)�$��0q�$��Center=�$��Center=�  ButtonStyle#���  $��8q�$��Bottom=� � Cursor$
 Hand���� -	    ��Name$ begin��$��Rename��$��Center=�$��Bottom=�$	��115��$��30��$��0q����	#FF648CE1$��0q�$��Segoe UI Semibold)�$��18(�
�������#���  $��8q�$��1��$
 Hand��� -
    �Name$ notification��$��1��$��10,191,10,9q�$��1��$��90��.+��� 
����������	#FFE8E8E8�� -   $ nClose��$��X��$��Center=�$��Center=�$��60��  CloseButStyle#���  $��Segoe UI Semibold)�$��0q�
�������$��24��
�������$��
320,0,0,66q��� -   $ nText��$��0,25,0,0q�$
��Wrap=�$	 5,0q�$��CharacterEllipsis=�$��Right=�$	��351���� -    ��Name$ nTitle��$��Label��
�������$��0,0,0,1q� J� RenderTransformOrigin$ -0.579,0.556&�$��24��$��5,1,5,0q�$��Segoe UI Semibold)�$��14(����	#FF969696$��	0,0,60,66q��� -   ���	#FF0064C8$��2��$��0��$��0q� J� Effect 3  -System.Windows.Media.Effects.DropShadowEffect     Opacity$
 0.25Q�   
BlurRadius$ 3Q�   	Direction$	 270Q�   ShadowDepth$ 3Q��� -   $��X��$��1��$��Center=�$��50��$��0q����	#00E62828���	#FF9C9C9C$
��Bold"�
�������#���  $��0,2,0,0q�$��16(�$��Stretch=�$��Center=�$��	321,0,0,0q��� $ ��SHOW AUTO-RENAMER beta 5.1��$��	10,1,99,0q�$��Segoe UI Semibold)�$��14.667(�$��0q�$��Center=�$��Stretch=� .   .  
�������$��2���� -    ��Name$ dragdropOverlay�����	#FF7396F0 . $��2��$��1�����	#997396F0$
 None��$��3��$y�Hidden=��� -   $ LabelPreviewTitle��$��
10,10,30,0q�$��1��$	��Top=�$ 0.474,0.462&�$��Segoe UI Semibold)�$��13.333(�$��25�� . $	��8,0q�$��Center=�
��������� -    ��Name$ advancedTitleIcon��$��25��$��25��$��0,10,0,0q�$��1��$��Right=�$	��Top=�$5��/icons/ic_settings_applications_whitish_24dp.png��$
 Hand�� � ToolTip$ Advanced title��|�.  .  