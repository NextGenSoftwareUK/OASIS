#!/usr/bin/perl
# Per-provider static audit (stubs, fake success, shortcuts). Usage: perl Scripts/audit_providers.pl Providers > audit.tsv
use strict;
use warnings;
use File::Find;

my $root = shift or die "root?";
my @skip = qw(HoloOASIS.Desktop HoloOASIS.Unity Neo4jOASIS2 EdgeSQLiteOASIS CargoOASIS);

opendir(my $rd, $root) or die $!;
my @cats = grep { -d "$root/$_" && !/^\./ && $_ ne 'Template' } readdir $rd;
closedir $rd;

print join("\t", qw(category provider files loc overrides notsupported nullish emptyresult throws
  simulate random fakehash delay fornow realimpl todo placeholder comment_todo http fake sdkpkgs ifaces flagged_methods)), "\n";

for my $cat (sort @cats) {
  opendir(my $cd, "$root/$cat") or next;
  for my $dir (sort grep { /^NextGenSoftware\.OASIS\.API\.Providers\./ } readdir $cd) {
    my $short = $dir; $short =~ s/^NextGenSoftware\.OASIS\.API\.Providers\.//;
    next if grep { $short eq $_ } @skip;
    next if $short =~ /Tests$|TestHarnes$/;
    my $path = "$root/$cat/$dir";
    my (@cs, @proj);
    find({ no_chdir => 1, wanted => sub {
      return if $File::Find::name =~ m{/(bin|obj|TestProjects)/};
      push @cs, $File::Find::name if /\.cs$/;
      push @proj, $File::Find::name if /\.csproj$/;
    }}, $path);

    my %m = map { $_ => 0 } qw(loc overrides notsupported nullish emptyresult throws simulate random fakehash delay fornow realimpl todo placeholder comment_todo http);
    my (%ifaces, @flagged);
    for my $f (@cs) {
      open(my $fh, '<', $f) or next; local $/; my $src = <$fh>; close $fh;
      my $comments = join("\n", $src =~ m{//[^\n]*|/\*.*?\*/}gs);
      $m{comment_todo} += () = $comments =~ /\b(TODO|FIXME|HACK)\b/g;
      $m{realimpl}     += () = $comments =~ /real implementation|in production,? (you|this) would|would normally/gi;
      $m{placeholder}  += () = $comments =~ /placeholder|simplified|mock(ed)?\b|dummy|for now/gi;
      (my $code = $src) =~ s{/\*.*?\*/}{}gs;
      $code =~ s{//[^\n]*}{}g;
      $m{loc} += scalar grep { /\S/ } split /\n/, $code;
      $m{simulate} += () = $code =~ /simulat/gi;
      $m{random}   += () = $code =~ /new Random\(|Random\.Shared/g;
      $m{fakehash} += () = $code =~ /"0x"\s*\+\s*Guid|Guid\.NewGuid\(\)\.ToString\("N"\)\s*[;+]/g;
      $m{delay}    += () = $code =~ /Task\.Delay\(/g;
      $m{fornow}   += () = $code =~ /placeholder|not yet implemented|coming soon/gi;
      $m{todo}     += () = $code =~ /\bTODO\b/g;
      $m{http}     += () = $code =~ /HttpClient|GraphQL|RpcAsync|PostAsync|GetAsync/g;
      while ($code =~ /class\s+\w+\s*(?:\([^)]*\))?\s*:\s*([^{]+)\{/g) { $ifaces{$_}++ for grep { /^IOASIS/ } map { s/\s+//gr } split /,/, $1; }
      while ($code =~ /public\s+override\s+(?:async\s+)?([\w<>\[\], ]+?)\s+(\w+)\s*\(([^)]*)\)\s*(=>\s*[^;]+;|\{)/g) {
        my ($ret, $name, $full) = ($1, $2, $4);
        $m{overrides}++;
        my $body = $full;
        if ($full eq '{') {
          my $start = pos($code); my $depth = 1; my $i = $start;
          while ($depth && $i < length $code) { my $c = substr($code, $i++, 1); $depth++ if $c eq '{'; $depth-- if $c eq '}'; }
          $body = substr($code, $start, $i - $start);
        }
        my $why;
        if ($body =~ /not supported|NotSupported|does not support|isn't supported|is not supported|not available/i && $body !~ /await\s+\w|\.(Get|Post|Put|Send|Delete)Async|Rpc|Query|Execute/) { $m{notsupported}++; $why = 'notsupported'; }
        elsif ($body =~ /^\s*\{?\s*return\s+(null|default(\(\w+\))?)\s*;\s*\}?\s*$/s) { $m{nullish}++; $why = 'null'; }
        elsif ($body =~ /throw new NotImplementedException|throw new NotSupportedException/) { $m{throws}++; $why = 'throws'; }
        elsif ($body !~ /await|Async\(|GetAwaiter|_http|_client|Client\.|HttpClient|Rpc|Query|Execute|Collection|_db|Connection|Command|Session|File\.|Directory\.|Stream|Send|Request|HandleError|IsError\s*=\s*true|throw|\b(Load|Save|Delete|Search|Export|Import|Get|Mint|Burn|Lock|Unlock)\w*\(|base\.|_provider|Manager|_store|_cache|_repo|_kv|_contract|_web3|_rpc/ && $body =~ /Result\s*=\s*(new|true|\w+)|=>\s*new OASISResult/ && $name !~ /^(Activate|DeActivate)Provider/) { $m{fake}++; $why = 'fake'; }
        elsif ($ret =~ /OASISResult/ && $body !~ /\.Result\s*=|Result\s*=|return\s+\w+Async\(|return\s+await|\.Result;|HandleError|=>\s*\w+Async\(|=>\s*\w+\(|GetAwaiter|IsError\s*=\s*true/ && $name !~ /^(Activate|DeActivate)Provider/) { $m{emptyresult}++; $why = 'emptyresult'; }
        push @flagged, "$name:$why" if $why;
      }
    }
    my $sdk = 0;
    for my $p (@proj) {
      open(my $fh, '<', $p) or next; local $/; my $x = <$fh>; close $fh;
      $sdk += () = $x =~ /<PackageReference Include="(?!NextGenSoftware\.|Newtonsoft|System\.|Microsoft\.(Extensions|NET|CSharp|SourceLink)|coverlet|xunit|NUnit|MSTest)[^"]+"/g;
    }
    my %seen; my @fl = grep { !$seen{$_}++ } @flagged;
    print join("\t", $cat, $short, scalar(@cs), @m{qw(loc overrides notsupported nullish emptyresult throws simulate random fakehash delay fornow realimpl todo placeholder comment_todo http fake)}, $sdk, join(',', sort keys %ifaces), join(' ', @fl[0 .. ($#fl < 11 ? $#fl : 11)])), "\n";
  }
  closedir $cd;
}
