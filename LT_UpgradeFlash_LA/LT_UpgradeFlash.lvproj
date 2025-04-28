<?xml version='1.0' encoding='UTF-8'?>
<Project Type="Project" LVVersion="17008000">
	<Item Name="My Computer" Type="My Computer">
		<Property Name="server.app.propertiesEnabled" Type="Bool">true</Property>
		<Property Name="server.control.propertiesEnabled" Type="Bool">true</Property>
		<Property Name="server.tcp.enabled" Type="Bool">false</Property>
		<Property Name="server.tcp.port" Type="Int">0</Property>
		<Property Name="server.tcp.serviceName" Type="Str">My Computer/VI Server</Property>
		<Property Name="server.tcp.serviceName.default" Type="Str">My Computer/VI Server</Property>
		<Property Name="server.vi.callsEnabled" Type="Bool">true</Property>
		<Property Name="server.vi.propertiesEnabled" Type="Bool">true</Property>
		<Property Name="specify.custom.address" Type="Bool">false</Property>
		<Item Name="Global.vi" Type="VI" URL="../Global.vi"/>
		<Item Name="LT_UpgradeFlash.lvclass" Type="LVClass" URL="../LT_UpgradeFlash.lvclass"/>
		<Item Name="Dependencies" Type="Dependencies">
			<Item Name="LT_UpgradeFlash_dll.dll" Type="Document" URL="../../New_Lib/LT_UpgradeFlash_dll/LT_UpgradeFlash_dll/bin/Debug/LT_UpgradeFlash_dll.dll"/>
		</Item>
		<Item Name="Build Specifications" Type="Build"/>
	</Item>
</Project>
