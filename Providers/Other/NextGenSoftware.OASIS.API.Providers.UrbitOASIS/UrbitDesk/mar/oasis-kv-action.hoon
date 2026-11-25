::  JSON form: {"put":{"key":"..","value":".."}} or {"del":{"key":".."}}
::
/-  oasis-kv
|_  act=action:oasis-kv
++  grad  %noun
++  grow
  |%
  ++  noun  act
  --
++  grab
  |%
  ++  noun  action:oasis-kv
  ++  json
    =,  dejs:format
    %-  of
    :~  [%put (ot ~[key+so value+so])]
        [%del (ot ~[key+so])]
    ==
  --
--
