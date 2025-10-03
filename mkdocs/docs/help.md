
# Help

If you are encountering a problem, here are some resources that may help you.

## 💬 Discussion Forum

[Search the Discussion Forum](https://github.com/philosowaffle/peloton-to-garmin/discussions) to see if your topic has already been discussed before.  Take note that the search bar lets you filter by `open` and `closed` discussions, it is beneficial to check both.

!!! tip "Have a question? Need help with something?"

    The [Discussion Forum](https://github.com/philosowaffle/peloton-to-garmin/discussions) is the best place to post.  When posting please ensure you are [providing the right details](#providing-the-right-details).

## 🐛 Issues

Bugs, feature requests, and more often get tracked in the [Issues](https://github.com/philosowaffle/peloton-to-garmin/issues) tab in Github. This is also a great place to search for information.  Particularly important issues that impact many people will usually be pinned and highly visible.  Take note that the search bar lets you filter by `open` and `closed` issues, it is beneficial to check both.

!!! tip "Have a feature idea? Encountered a bug?"

    Creating a new [Issue](https://github.com/philosowaffle/peloton-to-garmin/issues) is the best place to post.  When posting please ensure you are [providing the right details](#providing-the-right-details).

## 💁‍♂️ Providing the right details

P2G can be run in a variety of different ways and its difficult for people to provide help if they have to guess about your particular setup.  When asking for help, or logging a bug, please be sure to include the below information at a minimum for the most effective help:

1. How are you running P2G? In other words, which [install mtehod](install/index.md) did you choose when you setup P2G?
1. What [version of P2G](#finding-version) are you running?
1. A copy of the log files that were generated, ideally the entire log file, not just the part where you see an error. See [Finding logs](#finding-logs).

## 📋 Finding logs

### Headless / Docker Headless

The log files will be written to `output/p2g_log<datetime>.txt`.

### Windows UI / Web UI

Naviate to the `About` page, then click the `Logs` tab.  There may be more than one tab, be sure to copy all the logs from both tabs.  Additionally, the log files will be written to `output/p2g_log<datetime>.txt`.

In the UI, you can also temporarily increase the logging verbosity which can be helpful to capture even more details about a reproduceable error.  Simply increase the verbosity to `Verbose` and try to reproduce the issue, then check back to see what new logs were written.

### GitHub Actions

If your forked repository is public, then you can simply provide a link to the repository and people can view the necessary logs from there.  If your repository is private then to find the logs for a given run:

1. Go to your copy of the repository
1. Select the `Actions` tab, along the top left of the view
1. Click the very first `Sync workflow`` item to open up that workflow
1. In the left hand menu, under Jobs click `sync`
1. Finally, expand the item that says `Run /app/PelotonToGarminConsole`
1. Share the logs from this view

## #️⃣ Finding version

Version information is always written as the first thing in the [log file](#finding-logs).  Additionally, on a UI version of P2G you can find the version information on the `About` page.
