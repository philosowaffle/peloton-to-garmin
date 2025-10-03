# Welcome to MkDocs

For full documentation visit [mkdocs.org](https://www.mkdocs.org).
MkDocs: https://www.mkdocs.org/user-guide/installation/
Material design: https://squidfunk.github.io/mkdocs-material/getting-started/
Mike: https://github.com/jimporter/mike

## Commands

* `mkdocs new [dir-name]` - Create a new project.
* `mkdocs serve` - Start the live-reloading docs server.
* `mkdocs build` - Build the documentation site.
* `mkdocs -h` - Print help message and exit.

## Updating an existing version

* Checkout version of master branch that has the docs you want to modify
* Make changes
* `git fetch origin gh-pages --depth=1`
* `mike deploy <version> --push`
    * this will push to `gh-pages` branch and update the appropriate directory

## Project layout

    mkdocs.yml    # The configuration file.
    docs/
        index.md  # The documentation homepage.
        ...       # Other markdown pages, images and other files.
