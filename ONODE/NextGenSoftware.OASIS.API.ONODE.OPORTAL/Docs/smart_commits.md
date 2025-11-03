# Smart Commit commands

The basic syntax for a Smart Commit message is:

```
<ignored text> <ISSUE_KEY> <ignored text> <COMMAND> <optional COMMAND_ARGUMENTS>
```

Any text between the issue key and the command is ignored.
There are three commands you can use in your Smart Commit messages:

<br>

- `comment`
- `time`
- `transition`

  <br>

## Comment <br>

<br>

<table>
<tr>
<td>

**Description**

</td>
<td> 

Adds a comment to a Jira Software issue. 

</td> 
</tr>
<tr>
<td> 

Syntax 

</td>
<td>

```
<ignored text> <ISSUE_KEY> <ignored text> comment <comment_string>
```

</td>
</tr>
<tr>
<td> 

Example 

</td>
<td>

```
JRA-34 comment corrected indent issue
```

</td>
</tr>
<tr>
</table>

### Notes:

The committer's email address must match the email address of a single Jira Software user with permission to comment on issues in that particular project.


<br>

## Time

<br>
<table>
<tr>
<td>

**Description**

 </td>
 <td> 
 
 Records time tracking information against an issue. 
 
 </td> 
</tr>
<tr>
<td> 

Syntax 

</td>
<td>

```
<ignored text> ISSUE_KEY <ignored text> time <value>w <value>d <value>h <value>m <comment_string>
```

</td>
</tr>
<tr>
<td> 

Example 

</td>
<td>

```
JRA-34 time 1w 2d 4h 30m Total work logged
```

</td>
</tr>
</table>

### Notes:

This example records 1 week, 2 days, 4 hours and 30 minutes against the issue, and adds the comment <code>Total work logged</code> in the **Work Log** tab of the issue.

<br>

- Each value for <code>w</code>, <code>d</code>, <code>h</code> and <code>m</code> can be a decimal number.
- The committer's email address must match the email address of a single JIRA Software user with permission to log work on an issue.
- Your system administrator must have enabled time tracking on your JIRA Software instance.

  <br>


## Workflow transitions

<br>

<table>
<tr>
<td>

**Description**

 </td> 
 <td> 
 
 Transitions a Jira Software issue to a particular workflow state. 
 
 </td> 
</tr>
<tr>
<td> 

Syntax 

</td>
<td>

```
<ignored text> ISSUE_KEY <ignored text> <transition_name> <comment_string>
```

</td>
</tr>
<tr>
<td> 

Example 

</td>
<td>

```
JRA-090 #close Fixed this today
```

</td>
</tr>
</table>

### Notes:

This example executes the close issue workflow transition for the issue and adds the comment <code> Fixed this today </code> to the issue. Note that the comment is added automatically without needing to use the comment command.

You can see the custom commands available for use with Smart Commits by visiting the JIRA Software issue and seeing its available workflow transitions:

1. Open an issue in the project.
2. Click **View Workflow** (near the issue's **Status**).

The Smart Commit only considers the part of a transition name before the first space. So, for a transition name such as <code>finish work</code>, then specifying <code>#finish</code> is sufficient. You must use hyphens to replace spaces when ambiguity can arise over transition names, for example: <code>#finish-work</code>.

If a workflow has two valid transitions, such as:

- <code> Start Progress </code>
- <code> Start Review </code>

A Smart Commit with the action <code>#start</code> is ambiguous because it could mean either of the two transitions. To specify one of these two transitions, fully qualify the transition you want by using either <code>#start-review</code> or <code>#start-progress</code>. </code>

- When you resolve an issue with the <code>#resolve</code> command, you cannot set the **Resolution** field with Smart Commits.</code>
- If you want to add a comment during the transition, the transition must have a screen associated with it.
- The committer's email address must match the email address of a single JIRA Software user with the appropriate project permissions to transition issues.

  <br>


# Advanced examples
<br>

## Multiple commands on a single issue

<br>
<table>
<tr>
<td> 

Syntax

</td> 
<td>

```
<ISSUE_KEY> #<COMMAND_1> <optional COMMAND_1_ARGUMENTS> #<COMMAND_2> <optional COMMAND_2_ARGUMENTS> ... #<COMMAND_n> <optional COMMAND_n_ARGUMENTS>
```

</td>
</tr>
<tr>
<td>

Commit message 

</td>
<td>

```
JRA-123 #time 2d 5h #comment Task completed ahead of schedule #resolve
```

</td>
</tr>
<tr>
<td> 

Result 

</td> 
<td>

Logs 2 days and 5 hours of work against issue JRA-123, adds the comment 'Task completed ahead of schedule', and resolves the issue.

<br>


</td>
</tr>
</table>

-------------------------------------------------------
 
## Multiple commands over multiple lines on a single issue

<br>
<table>
<tr>
<td> 

Syntax 

</td> 
<td>

```
<ISSUE_KEY> #<COMMAND_1> <optional COMMAND_1_ARGUMENTS> #<COMMAND_2> <optional COMMAND_2_ARGUMENTS> ... #<COMMAND_n> <optional COMMAND_n_ARGUMENTS>
```

</td>
</tr>
<tr>
<td> 

Commit message 

</td>
<td>

```
JRA-123 #comment Imagine that this is a really, and I mean really, long comment #time 2d 5h
```

</td>
</tr>
<tr>
<td> 

Result 

</td> 
<td>

Adds the comment <code>'Imagine that this is a really, and I'</code>, but drops the rest of the comment. The work time of 2 days and 5 hours is not logged against the issue because there is no issue key for the time command in the second line. That is, each line in the commit message must conform to the Smart Commit syntax.

This example would work as expected if set out as:

```
JRA-123 #comment Imagine that this is a really, and I mean really, long comment
JRA-123 #time 2d 5h
```

</td>
</tr>
</table>
<br>
<br>

---------------------------------------------------------
## A single command on multiple issues

<br>
<br>
<table>
<tr>
<td> 

Syntax 

</td>
<td>

```
<ISSUE_KEY1> <ISSUE_KEY2> <ISSUE_KEY3> #<COMMAND> <optional COMMAND_ARGUMENTS> etc
```

</td>
</tr>
<tr>
<td> 

Commit message 

</td>
<td>

```
JRA-123 JRA-234 JRA-345 #resolve
```

</td>
</tr>
<tr>
<td> 

Result 

</td> 
<td> 

Resolves issues <code>JRA-123</code>, <code>JRA-234</code> and <code>JRA-345</code>. 

<br>

Multiple issue keys must be separated by whitespace or commas.

</td>
</tr>
</table>
<br>
<br>

## Multiple commands on multiple issues
<br> 

<table>
<tr>
<td> 

Syntax

 </td> 
<td>

```
<ISSUE_KEY1> <ISSUE_KEY2> ... <ISSUE_KEYn> #<COMMAND_1> <optional COMMAND_1_ARGUMENTS> #<COMMAND_2> <optional COMMAND_2_ARGUMENTS> ... #<COMMAND_n> <optional COMMAND_n_ARGUMENTS>
```

</td>
</tr>
<tr>
<td> 

Commit message 

</td>
<td>

```
JRA-123 JRA-234 JRA-345 #resolve #time 2d 5h #comment Task completed ahead of schedule
```

</td>
</tr>
<tr>
<td> 

Result 

</td>
<td>

Logs 2 days and 5 hours of work against issues <code>JRA-123</code>, <code>JRA-234</code> and <code>JRA-345</code>, adds the comment <code>'Task completed ahead of schedule'</code> to all three issues, and resolves all three issues.

Multiple issue keys must be separated by whitespace or commas.
</td>
</tr>
</table>