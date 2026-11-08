"""Create three owned synthetic native pages once, verify full/subset and restart.

Native agent protocol proof only: the .NET HTTP client and CQL remain separate.
"""
import hashlib
import json
import re

from qualify_urbit_contacts_install import main


def qualify_book(context, http_reader=None):
    root, out, report = (context[key] for key in ('root', 'out', 'report'))
    execute, prefix, suffix = (context[key] for key in ('execute', 'prefix', 'suffix'))
    path = root / 'Providers/Other/TestProjects/NextGenSoftware.OASIS.API.Providers.UrbitOASIS.UnitTests/Fixtures/urbit-contacts-book-source.json'
    raw = path.read_bytes()
    assert hashlib.sha256(raw).hexdigest() == '53c7954266ddecb261bcda68637e02a16ef1d89651997a0ef4fa168f4d0b71bb'
    fixture = json.loads(raw)
    assert fixture['visibility'] == 'Private' and [p['id'] for p in fixture['pages']] == ['0v1', '0v2', '0v3']
    report.update(nativeBookAgentQualified=False, nativeBookRestartVerified=False, nativeSeedCount=0)

    def fields(page):
        entries = []
        assert set(page['overlay']) == {'nickname', 'bio', 'status'}
        for name, value in sorted(page['overlay'].items()):
            assert re.fullmatch('[A-Za-z0-9 ]{1,128}', value), 'Only fixed synthetic fixture text may be inserted.'
            entries.append('[%' + name + " [%text '" + value + "']]")
        return '(my ~[' + ' '.join(entries) + '])'

    # Verify empty native book before any mutation, not an HTTP error interpreted as absence.
    execute('book-empty', prefix + ';<  book=*  bind:m  (scry * %gx /contacts/v1/book)  ?>  =(book ~)  ' + suffix)
    for index, page in enumerate(fixture['pages'], 1):
        intent = dict(owner=report['owner'], sourceOwner=fixture['sourceOwner'], ship='~zod',
                      fixtureSha256=hashlib.sha256(raw).hexdigest(), page=page,
                      beforeDispatch=True, nativeAcknowledged=False)
        with (out / ('seed-' + str(index) + '-intent.json')).open('x') as stream:
            json.dump(intent, stream, indent=2)
        body = prefix + ';<  ~  bind:m  (poke [our.bowl %contacts] contact-action-1+!>([%page [%id ' + page['id'] + '] ' + fields(page) + ']))  ' + suffix
        execute('seed-' + str(index), body)
        with (out / ('seed-' + str(index) + '-ack.json')).open('x') as stream:
            json.dump(dict(owner=report['owner'], id=page['id'], nativeAcknowledged=True), stream)
        report['nativeSeedCount'] += 1

    def verify(phase):
        for group, ids in [('full', [p['id'] for p in fixture['pages']]), ('subset', fixture['subsetIds'])]:
            for index, cid in enumerate(ids, 1):
                page = next(p for p in fixture['pages'] if p['id'] == cid)
                body = prefix + ';<  page=*  bind:m  (scry * %gx /contacts/v1/book/id/' + cid + ')  ?>  =(page [~ ' + fields(page) + '])  ' + suffix
                execute(phase + '-' + group + '-' + str(index), body)
        report[phase + 'FullCount'] = 3
        report[phase + 'SubsetCount'] = 2

    verify('beforeRestart')
    before = http_reader(context, 'beforeRestart') if http_reader is not None else None
    context['restart']()
    verify('afterRestart')
    if http_reader is not None:
        after = http_reader(context, 'afterRestart')
        assert before['projectedSourceSha256'] == after['projectedSourceSha256']
        report['nativeHttpClientQualified'] = True
        report['nativeContactsQualified'] = True
        report['nativeHttpSourcePreservedAcrossRestart'] = True
    report['nativeBookAgentQualified'] = True
    report['nativeBookRestartVerified'] = True
    # HTTP SDK / CQL flags deliberately stay false until their actual gates run.


if __name__ == '__main__':
    main(on_initialized=qualify_book)
