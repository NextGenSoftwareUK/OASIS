"""Exact Docker absence contracts for ownership-checked cloud test resources."""

def missing_resource(kind, name, returncode, stderr):
    if returncode != 1:
        return False
    text = stderr.strip().lower()
    name = name.lower()
    if kind == 'network':
        return text == 'error response from daemon: network ' + name + ' not found'
    if kind == 'container':
        return text in ['error: no such container: ' + name,
                        'error response from daemon: no such container: ' + name]
    if kind == 'volume':
        return text == 'error response from daemon: get ' + name + ': no such volume'
    raise ValueError('Unknown owned Docker resource kind.')
