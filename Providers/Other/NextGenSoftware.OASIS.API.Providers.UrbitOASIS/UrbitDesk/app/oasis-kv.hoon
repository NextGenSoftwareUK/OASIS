::  %oasis-kv: owner-only key/value state for the OASIS UrbitOASIS provider.
::
::  Pokes: %oasis-kv-action from our own ship only.
::  Scries (JSON), keys and prefixes passed as lowercase hex of their UTF-8:
::    /x/ping            "oasis-kv"
::    /x/entry/<hex>     the value as a string, or null when absent
::    /x/keys            every key
::    /x/keys/<hex>      keys starting with the prefix
::
/-  oasis-kv
/+  default-agent
|%
+$  state-0  [%0 kv=(map @t @t)]
+$  card  card:agent:gall
++  unhex
  |=  h=@ta
  ^-  @t
  ?>  =(0 (mod (met 3 h) 2))
  (rev 3 (div (met 3 h) 2) (rash h hex))
--
=|  state-0
=*  state  -
^-  agent:gall
|_  =bowl:gall
+*  this  .
    def   ~(. (default-agent this %|) bowl)
++  on-init  `this
++  on-save  !>(state)
++  on-load
  |=  old=vase
  ^-  (quip card _this)
  `this(state !<(state-0 old))
++  on-poke
  |=  [=mark =vase]
  ^-  (quip card _this)
  ?>  =(src.bowl our.bowl)
  ?>  ?=(%oasis-kv-action mark)
  =/  act  !<(action:oasis-kv vase)
  ?-  -.act
    %put  `this(kv (~(put by kv) key.act value.act))
    %del  `this(kv (~(del by kv) key.act))
  ==
++  on-peek
  |=  =path
  ^-  (unit (unit cage))
  ?+  path  (on-peek:def path)
      [%x %ping ~]
    ``json+!>(`json`s+'oasis-kv')
  ::
      [%x %entry @ ~]
    =/  val  (~(get by kv) (unhex i.t.t.path))
    ``json+!>(`json`?~(val ~ s+u.val))
  ::
      [%x %keys ~]
    ``json+!>(`json`a+(turn ~(tap in ~(key by kv)) |=(k=@t s+k)))
  ::
      [%x %keys @ ~]
    =/  pre=@t  (unhex i.t.t.path)
    =/  len  (met 3 pre)
    =/  keys  (skim ~(tap in ~(key by kv)) |=(k=@t =(pre (end [3 len] k))))
    ``json+!>(`json`a+(turn keys |=(k=@t s+k)))
  ==
++  on-watch  on-watch:def
++  on-leave  on-leave:def
++  on-agent  on-agent:def
++  on-arvo   on-arvo:def
++  on-fail   on-fail:def
--
