# Screenshots & demo assets

Drop your captures here with these **exact filenames** — the main `README.md`
already references them, so they'll appear automatically once added.

## How to add images on GitHub (easiest)
You don't even need to commit locally: open `README.md` on github.com → click the
pencil (edit) → **drag-and-drop** an image into the text. GitHub uploads it and
inserts the link. Or `git add docs/screenshots/*.png` and push.

## Shot list

Capture these from the running app (log in as `doctor@apollo.com` / `Test@123`
locally, or your live demo). Use a clean browser window, ~1440px wide.

| Filename | Screen | How to get there |
|---|---|---|
| `demo.gif` | 20–30s walkthrough | Record login → dashboard → open a patient → History tab → switch language. See below. |
| `dashboard.png` | Home dashboard | Log in → lands on `/dashboard` (widgets + Recent Alerts + Upcoming Transmissions) |
| `patient-detail.png` | Patient detail | Patients → click a patient → **History** tab (shows the charts) |
| `patients-list.png` | Advanced search | Patients → expand **Advanced Search**, run a filter |
| `i18n-french.png` | French UI | Login page → set Country/Language to France, or header language switcher |
| `hospitals.png` *(optional)* | Super-admin | Log in as SuperAdmin → **Hospitals** page |
| `audit-log.png` *(optional)* | Audit log | Log in as Admin → **Audit Log** |

## Recording the demo GIF

**Windows:** use the built-in **Xbox Game Bar** (`Win + G`) or [ScreenToGif](https://www.screentogif.com/) (free) to record a short clip, export as GIF.

Keep it **under ~10 MB** and ~800px wide so it loads fast in the README. A good
30-second flow:

1. Login screen (show the country/language picker)
2. Dashboard with the widgets
3. Open a patient → History tab (charts animate in)
4. Switch the UI language to French
5. Back to the patient list with a search filter

> Tip: a crisp GIF at the very top of the README is the single highest-impact
> thing you can add for an interviewer skimming the repo.
