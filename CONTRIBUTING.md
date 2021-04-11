# Contributing

Enhancements and fixes are always welcome. Feel free to contribute to any of the Issues not already assigned to another person.

* `pip install -r dev_requirements.txt`
* `cd tests`
* `pytest`
* New code should have unit tests coverage

# Python
```
> cd python
> edit python script
> pip install -r requirements.txt
> pip install pyinstaller
> pyinstaller -n upload --distpath ./ --console --clean --noconfirm upload.py
```