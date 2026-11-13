"""Throwaway test harness: renders the real Razor views to static HTML with sample data
so the real klenz.css / klenz.js can be checked in a browser without the .NET SDK.
It understands only the small subset of Razor these views use."""
import re, os, sys, json, itertools

ROOT = sys.argv[1]
OUT = os.path.join(ROOT, 'design-preview', 'app')
os.makedirs(OUT, exist_ok=True)

PAGES = {  # output file -> (view path, controller, action)
    'index.html': ('Views/Home/Index.cshtml', 'Home', 'Index'),
    'enquiries.html': ('Views/SalesEnquiries/Index.cshtml', 'SalesEnquiries', 'Index'),
    'enquiry-new.html': ('Views/SalesEnquiries/Create.cshtml', 'SalesEnquiries', 'Create'),
    'enquiry-edit.html': ('Views/SalesEnquiries/Edit.cshtml', 'SalesEnquiries', 'Edit'),
    'enquiry-details.html': ('Views/SalesEnquiries/Details.cshtml', 'SalesEnquiries', 'Details'),
    'enquiry-delete.html': ('Views/SalesEnquiries/Delete.cshtml', 'SalesEnquiries', 'Delete'),
    'quotations.html': ('Views/QuotationReports/Index.cshtml', 'QuotationReports', 'Index'),
    'quotation-new.html': ('Views/QuotationReports/Create.cshtml', 'QuotationReports', 'Create'),
    'projects.html': ('Views/ProjectLists/Index.cshtml', 'ProjectLists', 'Index'),
    'project-new.html': ('Views/ProjectLists/Create.cshtml', 'ProjectLists', 'Create'),
    'consultancy-new.html': ('Views/ProjectConsultancy/Create.cshtml', 'ProjectConsultancy', 'Create'),
    'tenders.html': ('Views/ProjectTenderList/Index.cshtml', 'ProjectTenderList', 'Index'),
    'tender-new.html': ('Views/ProjectTenderList/Create.cshtml', 'ProjectTenderList', 'Create'),
    'tender-details.html': ('Views/ProjectTenderList/Details.cshtml', 'ProjectTenderList', 'Details'),
    'tender-delete.html': ('Views/ProjectTenderList/Delete.cshtml', 'ProjectTenderList', 'Delete'),
    'companies.html': ('Views/CompanyNames/Index.cshtml', 'CompanyNames', 'Index'),
    'company-new.html': ('Views/CompanyNames/Create.cshtml', 'CompanyNames', 'Create'),
    'years.html': ('Views/FinancialYears/Index.cshtml', 'FinancialYears', 'Index'),
    'users.html': ('Views/Users/Index.cshtml', 'Users', 'Index'),
    'profile.html': ('Areas/Identity/Pages/Account/MyProfile.cshtml', '', 'MyProfile'),
    'login.html': ('Areas/Identity/Pages/Account/Login.cshtml', '', 'Login'),
    'register.html': ('Areas/Identity/Pages/Account/Register.cshtml', '', 'Register'),
}
LINKS = {(c, a): f for f, (_, c, a) in PAGES.items() if c}

COMPANIES = ['Northwind Textiles', 'Sample Foods Pvt Ltd', 'Demo Pharma', 'Example Paper Mills', 'Acme Dyeing Works',
             'Contoso Tanneries', 'Placeholder Steels', 'Test Breweries', 'Fabrikam Sugars', 'Initech Cements',
             'Globex Polymers', 'Umbrella Agro', 'Hooli Ceramics', 'Stark Fertilisers']
WORK = ['Effluent treatment chemicals for dyeing unit', 'Boiler water treatment, 2 TPH boiler', 'Cooling tower consultancy and dosing',
        'RO antiscalant trial for 50 KLD plant', 'Colour removal polymer, monthly supply', 'Sludge dewatering polyelectrolyte',
        'Descaling chemical for heat exchangers', 'CIP cleaning chemicals annual contract']
STATUS = ['New', 'Follow up', 'Quoted', 'Positive', 'Lost', 'Follow up', 'New', 'Quoted']
PEOPLE = ['Plant manager, 98xxxxxx01', 'Utilities head', 'Engineering lead', 'Purchase officer', 'Owner', 'ETP in-charge']
MONEY = ['₹ 4,50,000.00', '₹ 12,80,000.00', '₹ 86,500.00', '₹ 1,02,40,000.00', '₹ 6,40,000.00', '₹ 18,40,000.00']

def cell_sample(label, i):
    l = label.lower()
    if 'created date' in l: return f'{(i*3)%27+1:02d}/11/2026 {9+i%8:02d}:{(i*7)%60:02d}'
    if 'date' in l: return '' if ('reminder' in l and i % 3 == 2) else f'{(i*5)%27+1:02d}/{(i%3)+9:02d}/2026'
    if 'company' in l or l == 'full name': return COMPANIES[i % len(COMPANIES)]
    if 'short name' in l: return ''.join(w[0] for w in COMPANIES[i % len(COMPANIES)].split()).upper()
    if 'status' in l: return STATUS[i % len(STATUS)]
    if 'customer' in l: return PEOPLE[i % len(PEOPLE)]
    if any(k in l for k in ('value', 'cost', 'total')): return MONEY[i % len(MONEY)]
    if 'gst' in l: return ['18 %', '12 %', '5 %'][i % 3]
    if 'financial year' in l or l == 'f.y': return '2026-27'
    if 'duration' in l: return ['3 months', '45 days', '1 year'][i % 3]
    if 'place' in l: return ['Site visit', 'Phone call', 'Head office', ''][i % 4]
    if 'remark' in l: return ['', 'Wants a trial first', 'Revised offer sent', ''][i % 4]
    if 'user' in l: return ['admin', 'ravi', 'meena'][i % 3]
    if 'start' in l: return '01/04/2026'
    if 'end' in l: return '31/03/2027'
    if 'email' in l: return ['admin@example.com', '', 'meena@example.com'][i % 3]
    if 'phone' in l: return ['', '98xxxxxx12', ''][i % 3]
    return WORK[i % len(WORK)]

def find_block(s, start):
    """s[start] is '{'; return index after its matching '}'."""
    depth = 0
    for i in range(start, len(s)):
        if s[i] == '{': depth += 1
        elif s[i] == '}':
            depth -= 1
            if depth == 0: return i + 1
    raise ValueError('unbalanced braces')

REM = [dict(Id=i + 1, Company=COMPANIES[i], Place=['Site visit', 'Phone call', 'Head office'][i % 3], Details=WORK[i]) for i in range(6)]
MODEL = {
    'Overdue': REM[:1], 'DueToday': REM[1:3], 'Upcoming': REM[3:6],
    'funnel': [dict(Name='Enquiries', Count=128, Share='1', Step='45% move forward'), dict(Name='Quotations', Count=57, Share='0.445', Step='40% move forward'),
               dict(Name='Positive', Count=23, Share='0.18', Step='61% move forward'), dict(Name='Projects', Count=14, Share='0.109', Step='')],
    'RecentEnquiries': [dict(Id=i, Title=COMPANIES[i], Subtitle=WORK[i], Status=STATUS[i]) for i in range(6)],
    'RecentProjects': [dict(Id=i, Title=COMPANIES[i + 4], Subtitle=WORK[i + 2], Amount=MONEY[i]) for i in range(5)],
    'RecentTenders': [dict(Id=i, Title=['Municipal STP chemicals', 'Thermal plant DM water', 'Textile park CETP polymer', 'Refinery cooling water'][i], Subtitle=WORK[i + 3], Amount=MONEY[i + 1]) for i in range(4)],
}
EXPR = [  # (regex on expression text, value or callable(ctx, match))
    (r'^Model\.Enquiries$', '128'), (r'^Model\.Quotations$', '57'), (r'^Model\.Positive$', '23'), (r'^Model\.Projects$', '14'),
    (r'^Model\.Tenders$', '9'), (r'^Model\.UpcomingTenders$', '4'), (r'^Model\.Consultancy$', '6'),
    (r'^Model\.FinancialYear$', '2026-27'), (r'^Model\.StartDate', '1 Apr 2026'), (r'^Model\.EndDate', '31 Mar 2027'),
    (r'^Inr\(Model\.QuotedValue\)$', '₹4.82 Cr'), (r'^Inr\(Model\.WonValue\)$', '₹1.36 Cr'), (r'^Rate\(', '11%'),
    (r'attention == 0', '3 follow-ups need attention'), (r'Model\.Quotations == 1', 'quotations'), (r'Model\.Projects == 1', 'projects'),
    (r'^Model\.(Overdue|DueToday|Upcoming)\.Count$', lambda c, m: str(len(MODEL[m.group(1)]))),
    (r'firstTab == "over" \? "on"', ''), (r'firstTab == "today" \? "on"', 'on'), (r'^firstTab != "over"$', 'hidden'), (r'^firstTab != "today"$', ''),
    (r'Place != "" &&', '·'), (r'Title\.Length > 1', lambda c, m: c['e']['Title'][:2].upper()),
    (r'Date\.HasValue \?', lambda c, m: '%02d Nov' % (11 - list(c.values())[-1]['Id'])), (r'^r\.Date\.ToString\("ddd dd"\)$', 'Fri 13'), (r'^r\.Date', '09 Nov'),
    (r'Amount\.HasValue \? Inr', lambda c, m: list(c.values())[-1]['Amount'].replace('₹ ', '₹').replace('.00', '')),
    (r'^Html\.Raw\(Json\.Serialize\(Model\.Months\)\)$', json.dumps([dict(label=l, quoted=q * 100000, won=w * 100000) for l, q, w in zip(
        ['Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov'], [38, 52, 45, 61, 58, 72, 66, 90], [10, 14, 12, 20, 15, 22, 18, 25])])),
    (r'^ViewBag\.ActiveFinancialYear$', '2026-27'), (r'^ViewBag\.FyId$', '3'), (r'^TempData', ''), (r'^Model\.ReturnUrl$', ''),
    (r'CreatingForSomeoneElse \? "New user" : "Create the first', 'Create the first account'),
    (r'CreatingForSomeoneElse\s*\? "Create a sign-in', 'This account becomes the administrator and can add everyone else.'),
    (r'CreatingForSomeoneElse \? "Create user"', 'Create account'),
    (r'item\.Id == currentUserId', lambda c, m: ' (you)' if c['item']['i'] == 0 else ''),
    (r'item\.IsAdmin == 1 \? "Remove admin"', lambda c, m: 'Remove admin' if c['item']['i'] == 0 else 'Make admin'),
    (r'^item\.UserName$', lambda c, m: ['admin', 'ravi', 'meena', 'suresh'][c['item']['i'] % 4]),
    (r'^item\.Email$', lambda c, m: ['admin@example.com', '', 'meena@example.com', ''][c['item']['i'] % 4]),
    (r'^item\.PhoneNumber$', lambda c, m: ['', '98xxxxxx12', '', ''][c['item']['i'] % 4]),
    (r'^Model\.(Name|Email|PhoneNumber|Address)$', lambda c, m: dict(Name='Sample User', Email='user@example.com', PhoneNumber='98xxxxxx00', Address='Coimbatore')[m.group(1)]),
    (r'^Model\.DateOfBirth', '12 Mar 1990'), (r'ProfilePicture != null', '/wwwroot/images/profile.png'), (r'^Url\.Content\(Model\.FilePath\)$', '#'),
]
COND = [(r'Count == 0', False), (r'!Model\.HasFinancialYear', False), (r'TempData\["TemporaryPassword"\]', True), (r'TempData', False),
        (r'^!canManage$', False), (r'canManage', True), (r'e\.Status != ""', True), (r'item\.IsAdmin == 1', lambda c: c['item']['i'] == 0),
        (r'CreatingForSomeoneElse', False), (r'IsActive == 1|IsPositive == 1|IsActiveBool', lambda c: c['item']['i'] % 4 != 3),
        (r'FilePath', lambda c: c.get('item', {'i': 0})['i'] % 2 == 0), (r'User\.Identity', True)]

unknown = set()
def value(expr, ctx):
    expr = expr.strip()
    for rx, v in EXPR:
        m = re.search(rx, expr)
        if m: return v(ctx, m) if callable(v) else v
    m = re.match(r'^(\w+)\??\.(\w+)', expr)
    if m and m.group(1) in ctx and isinstance(ctx[m.group(1)], dict) and m.group(2) in ctx[m.group(1)]:
        return str(ctx[m.group(1)][m.group(2)])
    m = re.match(r'^Html\.Display(Name)?For\(\w+ => \w+\.([\w.]+)\)$', expr)
    if m:
        label = re.sub(r'(?<=[a-z])(?=[A-Z])', ' ', m.group(2).split('.')[-1])
        return label if m.group(1) else cell_sample(label, ctx.get('item', {'i': 1})['i'])
    if expr.startswith('ViewData["Title"]'): return ctx.get('title', '')
    m = re.search(r'\b(item|Model)\??\.(\w+)', expr)
    if m:
        label = re.sub(r'(?<=[a-z])(?=[A-Z])', ' ', m.group(2))
        it = ctx.get('item', {'i': 1})
        if m.group(1) == 'item' and m.group(2) in it: return str(it[m.group(2)])
        return cell_sample(label, it['i'])
    unknown.add(expr)
    return cell_sample('details', 1) if expr.startswith(('Model', '(Model')) else ''

def cond(expr, ctx):
    for rx, v in COND:
        if re.search(rx, expr): return v(ctx) if callable(v) else v
    unknown.add('IF ' + expr)
    return True

def render(s, ctx):
    out, i = [], 0
    while True:
        j = s.find('@', i)
        if j < 0: out.append(s[i:]); break
        out.append(s[i:j])
        rest = s[j:]
        if j > 0 and s[j - 1].isalnum() and re.match(r'@[\w.]+\.\w+', rest):  # e-mail address
            out.append('@'); i = j + 1; continue
        m = re.match(r'@\*.*?\*@', rest, re.S)
        if m: i = j + m.end(); continue
        m = re.match(r'@foreach \(var (\w+) in ([\w.]+)\)\s*', rest)
        if m:
            b = j + m.end(); e = find_block(s, b); body = s[b + 1:e - 1]
            items = MODEL.get(m.group(2).replace('Model.', ''), [dict(i=k, Id=k + 1) for k in range(ctx.get('rows', 14))])
            for k, it in enumerate(items):
                it = dict(it); it.setdefault('i', k)
                out.append(render(body, {**ctx, m.group(1): it}))
            i = e; continue
        m = re.match(r'@if \((.*?)\)\s*\n?\s*(?=\{)', rest, re.S)
        if m:
            b = j + m.end(); e = find_block(s, b); body = s[b + 1:e - 1]
            keep = cond(m.group(1), ctx)
            m2 = re.match(r'\s*else\s*(?=\{)', s[e:])
            other = None
            if m2:
                b2 = e + m2.end(); e2 = find_block(s, b2); other = s[b2 + 1:e2 - 1]; e = e2
            out.append(render(body if keep else (other or ''), ctx))
            i = e; continue
        m = re.match(r'@\{', rest)
        if m:
            e = find_block(s, j + 1); i = e; continue
        if rest.startswith('@('):
            depth, k = 0, 1
            while True:
                if rest[k] == '(': depth += 1
                elif rest[k] == ')':
                    depth -= 1
                    if depth == 0: break
                k += 1
            out.append(value(rest[2:k], ctx)); i = j + k + 1; continue
        m = re.match(r'@(await )?([A-Za-z_]\w*(?:\[[^\]]*\])?(?:\??\.[A-Za-z_]\w*|\((?:[^()]|\([^()]*\))*\))*)', rest)
        if m:
            out.append(value(m.group(2), ctx)); i = j + m.end(); continue
        out.append('@'); i = j + 1
    return ''.join(out)

def tag_helpers(h, ctrl):
    def href(c, a):
        return LINKS.get((c, a), '#')
    h = re.sub(r'\s*asp-area="[^"]*"', '', h)
    h = re.sub(r'asp-validation-summary="\w+" class="', 'class="validation-summary-valid ', h)
    h = re.sub(r'asp-controller="(\w+)"\s+asp-action="(\w+)"', lambda m: f'href="{href(m.group(1), m.group(2))}"', h)
    h = re.sub(r'<a ([^>]*?)asp-action="(\w+)"', lambda m: f'<a {m.group(1)}href="{href(ctrl, m.group(2))}"', h)
    h = re.sub(r'<form ([^>]*?)asp-action="\w+"', r'<form \1action="#" onsubmit="return false"', h)
    h = re.sub(r'\s*asp-(route-\w+|page-handler|append-version|validation-summary|route-returnUrl)="[^"]*"', '', h)
    h = re.sub(r'asp-page="[^"]*"', 'href="#"', h)
    lab = lambda n: re.sub(r'(?<=[a-z])(?=[A-Z])', ' ', n.split('.')[-1]).replace(' Id', '').replace('Fy Year', 'Financial Year')
    h = re.sub(r'<label asp-for="([\w.]+)"([^>]*)></label>', lambda m: f'<label for="{m.group(1)}"{m.group(2)}>{lab(m.group(1))}</label>', h)
    h = re.sub(r'<label asp-for="([\w.]+)"', r'<label for="\1"', h)
    def sel(m):
        opts = COMPANIES[:6] if 'Compan' in m.group(1) else ['2026-27', '2025-26'] if 'Fy' in m.group(1) else ['18', '12', '5'] if 'GST' in m.group(1) else ['New', 'Follow up', 'Quoted']
        return m.group(0) + ''.join(f'<option>{o}</option>' for o in opts)
    h = re.sub(r'<select asp-for="([\w.]+)"[^>]*asp-items="[^"]*"[^>]*>', sel, h)
    h = re.sub(r'\s*asp-items="[^"]*"', '', h)
    def inp(m):
        n = m.group(2)
        t = '' if 'type=' in m.group(0) else (' type="date"' if 'Date' in n else ' type="checkbox"' if n.endswith(('RememberMe', 'IsActiveBool', 'IsPositiveBool')) else ' type="text"')
        num = ' data-val-number="x"' if re.search(r'Cost|Total|Value', n) and 'type=' not in m.group(0) else ''
        return f'<{m.group(1)}{t}{num} id="{n}" name="{n}"'
    h = re.sub(r'<(input|select|textarea) asp-for="([\w.]+)"', inp, h)
    h = re.sub(r'<span asp-validation-for="[\w.]+"', '<span', h)
    h = re.sub(r'<partial [^>]*/>', '', h)
    h = h.replace(' hidden=""', '').replace(' hidden="hidden"', ' hidden')
    import time
    h = h.replace('~/', '/wwwroot/')
    return re.sub(r'(klenz\.(?:css|js))"', r'\1?v=%d"' % time.time(), h)

layout = open(os.path.join(ROOT, 'Views/Shared/_Layout.cshtml'), encoding='utf-8-sig').read()
profile = open(os.path.join(ROOT, 'Views/Shared/Components/UserProfile/Default.cshtml'), encoding='utf-8-sig').read().replace('@model string', '').replace('@Model', '/wwwroot/images/profile.png')

def page(view, ctrl, action):
    src = open(os.path.join(ROOT, view), encoding='utf-8-sig').read().replace('\r\n', '\n')
    title = (re.search(r'ViewData\["Title"\] = "([^"]*)"', src) or [None, action])[1]
    src = re.sub(r'^@(page|model|inject|using)[^\n]*\n', '', src, flags=re.M)
    m = re.search(r'@functions\s*\{', src)
    if m: src = src[:m.start()] + src[find_block(src, m.end() - 1):]
    scripts = ''
    m = re.search(r'@section Scripts\s*\{', src)
    if m:
        e = find_block(src, m.end() - 1); scripts = src[m.end():e - 1]; src = src[:m.start()] + src[e:]
    src = re.sub(r'@section Styles\s*\{.*?\n\}', '', src, flags=re.S)
    ctx = {'title': title, 'rows': 3 if ctrl in ('FinancialYears',) else 4 if ctrl == 'Users' else 14}
    body = render(src, ctx)
    scripts = render(scripts, ctx)
    bare = action in ('Login', 'Register')
    lay = layout
    m = re.search(r'@if \(bare\)\s*(?=\{)', lay)
    b = m.end(); e = find_block(lay, b); a_body = lay[b + 1:e - 1]
    m2 = re.match(r'\s*else\s*(?=\{)', lay[e:]); b2 = e + m2.end(); e2 = find_block(lay, b2); b_body = lay[b2 + 1:e2 - 1]
    lay = lay[:m.start()] + (a_body if bare else b_body) + lay[e2:]
    lay = re.sub(r'@on\("(\w+)"\)', lambda mm: 'on' if mm.group(1) == ctrl else '', lay)
    lay = lay.replace('@(bare ? "auth" : "")', 'auth' if bare else '')
    lay = re.sub(r'@Url\.Action\("Reminders"[^)]*\)\)?', 'reminders.json', lay)
    lay = re.sub(r'@Url\.Action\("Search"[^)]*\)\)?', 'search.json', lay)
    lay = re.sub(r'@Url\.Action\("Details"[^)]*\)\)?', 'enquiry-details.html?', lay)
    lay = re.sub(r'@Url\.Action\("Create", "(\w+)"[^)]*\)\)?', lambda mm: LINKS.get((mm.group(1), 'Create'), '#'), lay)
    lay = lay.replace('@await Component.InvokeAsync("UserProfile")', profile)
    lay = lay.replace('@RenderBody()', body).replace('@RenderSection("Styles", required: false)', '')
    lay = lay.replace('@await RenderSectionAsync("Scripts", required: false)', scripts)
    lay = render(lay, ctx)
    return tag_helpers(lay, ctrl)

for out, (view, ctrl, action) in PAGES.items():
    try:
        html = page(view, ctrl, action)
    except Exception as ex:
        print('FAILED', out, repr(ex)); continue
    left = sorted(set(re.findall(r'(?<![\w.])@[A-Za-z(][^\s<"]{0,40}', html)) - {'@media'})
    open(os.path.join(OUT, out), 'w', encoding='utf-8').write(html)
    print(f'{out:24s} {len(html):7d} bytes', ('LEFTOVER ' + str(left[:6])) if left else '')

json.dump([dict(id=1, company='Contoso Tanneries', details='Sludge dewatering polyelectrolyte', place='Phone call', when='09 Nov', state='over'),
           dict(id=2, company='Northwind Textiles', details='Effluent treatment chemicals', place='Site visit', when='11 Nov', state='today'),
           dict(id=3, company='Sample Foods Pvt Ltd', details='Boiler water treatment', place='Phone call', when='11 Nov', state='today'),
           dict(id=4, company='Demo Pharma', details='Cooling tower consultancy', place='Head office', when='13 Nov', state='soon')],
          open(os.path.join(OUT, 'reminders.json'), 'w'))
json.dump([dict(id=1, company='Northwind Textiles', details='Effluent treatment chemicals for dyeing unit', status='Follow up'),
           dict(id=3, company='Demo Pharma', details='Cooling tower consultancy and dosing', status='Quoted')],
          open(os.path.join(OUT, 'search.json'), 'w'))
if unknown: print('UNKNOWN EXPRESSIONS:', *sorted(unknown)[:40], sep='\n  ')
