"""Fail closed if an upstream merge removes NimN's identity or country integration."""
from pathlib import Path
import hashlib
import json

ROOT = Path(__file__).resolve().parents[2]


def check():
    required = {
        'v2rayN/ServiceLib/Global.cs': ['AppName = "NimN"', '"Nim4a/NimN"'],
        'v2rayN/ServiceLib/Services/UpdateService.cs': ['NimNRelease.SelectTag'],
        'v2rayN/ServiceLib/ViewModels/ProfilesViewModel.cs': ['ServerCountryService.Instance.ResolveAsync'],
        'v2rayN/ServiceLib/Models/Dto/ProfileItemModel.cs': ['ServerCountryCode', 'CountryCode'],
        'v2rayN/v2rayN/Views/ProfilesView.xaml': ['<base:MyDGCountryColumn'],
        'v2rayN/v2rayN/Base/MyDGCountryColumn.cs': ['CountryCode'],
        'v2rayN/v2rayN.Desktop/Views/ProfilesView.axaml': ['CountryFlagConverter'],
        'v2rayN/v2rayN/v2rayN.csproj': ['<AssemblyName>NimN</AssemblyName>'],
        'v2rayN/v2rayN.Desktop/v2rayN.Desktop.csproj': ['<AssemblyName>NimN</AssemblyName>'],
    }
    for file, needles in required.items():
        text = (ROOT / file).read_text(encoding='utf-8-sig')
        for needle in needles:
            if needle not in text:
                raise RuntimeError(f'NimN customization lost: {file}: {needle}')
    for file, expected in json.loads((ROOT / '.github/nimn-assets.json').read_text()).items():
        if hashlib.sha256((ROOT / file).read_bytes()).hexdigest() != expected:
            raise RuntimeError(f'NimN icon/flag changed: {file}')
    print('NimN identity, updater, flags, and automatic lookup guards passed')


if __name__ == '__main__':
    check()
