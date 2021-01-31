﻿Imports Microsoft.VisualBasic.ApplicationServices.Development.XmlDoc.Assembly
Imports Microsoft.VisualBasic.Scripting.SymbolBuilder
Imports Microsoft.VisualBasic.Text.Xml.Models
Imports SMRUCC.Rsharp.Development
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Components.Interface
Imports SMRUCC.Rsharp.Runtime.Interop
Imports any = Microsoft.VisualBasic.Scripting

Public Class [function]

    Public Function createHtml(api As RFunction, env As Environment) As String
        If TypeOf api Is RMethodInfo Then
            Return createHtml(DirectCast(api, RMethodInfo), env)
        Else
            Throw New NotImplementedException(api.GetType.FullName)
        End If
    End Function

    Public Function createHtml(api As RMethodInfo, env As Environment) As String
        Dim xml As ProjectMember = env.globalEnvironment _
            .packages _
            .packageDocs _
            .GetAnnotations(api.GetRawDeclares)
        Dim func As New FunctionDeclare With {
            .name = api.name,
            .parameters = api.parameters _
                .Select(AddressOf argument) _
                .ToArray
        }
        Dim docs As New Document With {
            .declares = func
        }

        Return createHtml(docs)
    End Function

    Private Function argument(arg As RMethodArgument) As NamedValue
        Return New NamedValue With {
            .name = arg.name,
            .text = any.ToString(arg.default)
        }
    End Function

    Private Shared Function blankTemplate() As XElement
        Return <html>
                   <head>
                       <!-- Viewport mobile tag for sensible mobile support -->
                       <meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1"/>
                       <meta http-equiv="Content-Type" content="text/html" charset="UTF-8"/>
                       <meta name="description" content="{$abstract}"/>

                       <title>{$name_title} function | R Documentation</title>

                       <base href="https://www.rdocumentation.org"/>
                       <link href="{$canonical_link}" rel="canonical"/>

                       <!--STYLES-->
                       <link rel="stylesheet" href="/min/production.min.702e152d1c072db370ae8520b7e2d417.css"/>
                       <link href='https://fonts.googleapis.com/css?family=Open+Sans:400,300,300italic,400italic,600,600italic,700,700italic' rel='stylesheet' type='text/css'/>
                       <link rel="stylesheet" href="https://cdn.jsdelivr.net/simplemde/latest/simplemde.min.css"/>
                       <link rel="stylesheet" href='/css/nv.d3.min.css'/>
                       <link rel="stylesheet" href='/css/bootstrap-treeview.css'/>
                       <link rel="stylesheet" href='/css/bootstrap-glyphicons.css'/>
                       <!--STYLES END-->
                   </head>
                   <body>
                       <div id="content">


                           <section class="navbar navbar-color navbar-fixed-top">
                               <nav class="clearfix">
                                   <div class="navbar--title">
                                       <a href="/">
                                           <div class="logo"></div>
                                           <div class="logo-title"><span>RDocumentation</span></div>
                                       </a>
                                   </div>
                                   <ul class="navbar--navigation largescreen">
                                       <li><a href="https://www.datacamp.com/groups/business" class="btn btn-secondary-dark">R Enterprise Training</a></li>

                                       <li><a href="https://github.com/datacamp/Rdocumentation" class="btn btn-secondary">R package</a></li>
                                       <li><a href="/trends" class="btn btn-secondary">Leaderboard</a></li>


                                       <li><a href="/login?rdr=%2Fpackages%2Fregtomean%2Fversions%2F1.0%2Ftopics%2Flanguage_test" class="btn btn-primary">Sign in</a></li>


                                   </ul>

                                   <div class="navbar--search">
                                       <form class="search" action="/search" method="get">
                                           <input name="q" id="searchbar" type="text" placeholder="Search for packages, functions, etc" autocomplete="off"/>
                                           <input name="latest" id="hidden_latest" type="hidden"/>
                                           <div class="search--results"></div>
                                       </form>
                                   </div>
                               </nav>
                           </section>


                           <div class="page-wrap">







                               <section class="topic packageData"
                                   data-package-name="regtomean"
                                   data-latest-version="1.0"
                                   data-dcl='false'>

                                   <header class='topic-header'>
                                       <div class="container">

                                           <div class="th--flex-position">
                                               <div><!-- Do not remove this div, needed for th-flex-position -->
                                                   <h1>{$name_title}</h1>
                                               </div>
                                               <div><!-- Do not remove this div, needed for th-flex-position -->
                                                   <div class="th--pkg-info">
                                                       <div class="th--origin">
                                                           <span>From <a href="/packages/regtomean/versions/1.0">{$package}</a></span>

                                                           <span>by <a href="/collaborators/name/Daniela Recchia">{$author}</a></span>

                                                       </div>
                                                       <div class="th--percentile">


                                                           <div class="percentile-widget percentile-task" data-url="/api/packages/regtomean/percentile">
                                                               <span class="percentile-th">
                                                                   <span class='percentile'>0th</span>
                                                               </span>
                                                               <p>Percentile</p>
                                                           </div>


                                                       </div>
                                                   </div>
                                               </div>
                                           </div>
                                       </div>
                                   </header>


                                   <div class="container">
                                       <section>
                                           <h5>{$title}</h5>
                                           <p>{$summary}</p>
                                       </section>


                                       <section class="topic--keywords">
                                           <div class="anchor" id="l_keywords"></div>
                                           <dl>
                                               <dt>Keywords</dt>
                                               <dd><a href="/search/keywords/datasets">datasets</a></dd>
                                           </dl>
                                       </section>



                                       <section id="usage">
                                           <div class="anchor" id="l_usage"></div>
                                           <h5 class="topic--title">Usage</h5>
                                           <pre><code class="R">{$usage}</code></pre>
                                       </section>




                                       <!-- Other info -->







                                       <div class="anchor" id="l_sections"></div>


                                       <section>
                                           <h5 class="topic--title">Arguments</h5>
                                           <dl>

                                               <dt>Before</dt>
                                               <dd><p>a numeric vector giving the data values for the first (before) measure.</p></dd>

                                               <dt>After</dt>
                                               <dd><p>a numeric vector giving the data values for the second (after) measure.</p></dd>

                                               <dt>data</dt>
                                               <dd><p>an optional data frame containing the variables in the formula. By <code>default</code> the variables are taken from <code>environment (formula)</code>.</p></dd>

                                           </dl>
                                       </section>








                                       <section style="display: none;">
                                           <div class="anchor" id="alss"></div>
                                           <h5 class="topic--title">Aliases</h5>
                                           <ul class="topic--aliases">

                                               <li>{$name_title}</li>

                                           </ul>
                                       </section>



                                       <section>
                                           <div class="anchor" id="l_examples"></div>
                                           <h5 class="topic--title">Examples</h5>

                                           <pre><code class="R" data-package-name="{$package}">{$examples}</code></pre>

                                       </section>


                                       <small>
                                           <i> Documentation reproduced from package <span itemprop="name">{$package}</span>, version <span itemprop="version">1.0</span>,
        License: MIT + file LICENSE
      </i>
                                       </small>


                                   </div>
                               </section>

                           </div>

                           <div class="footer">
                               <a class="navbar--title apidoc btn btn-default js-external" target="_blank" href="/docs">
                                   <i class="fa fa-cogs"></i>
      API documentation
  </a>
                               <div class="navbar--title footer-largescreen pull-right">

                                   <a href="https://github.com/datacamp/rdocumentation" class="js-external">
                                       <div class="github"></div>
                                       <div class="logo-title">R package</div>
                                   </a>

                               </div>
                               <div class="navbar--title footer-largescreen pull-right">
                                   <a href="https://github.com/datacamp/rdocumentation-app" class="js-external">
                                       <div class="github"></div>
                                       <div class="logo-title">Rdocumentation.org</div>
                                   </a>
                               </div>
                               <div class="footer--credits--title">
                                   <p class="footer--credits">Created by <a href="https://www.datacamp.com" class="js-external">DataCamp.com</a></p>
                               </div>
                           </div>



                       </div>
                   </body>
               </html>
    End Function

    Public Function createHtml(docs As Document) As String
        With New ScriptBuilder(blankTemplate)
            !name_title = docs.declares.name
            !usage = docs.declares.ToString

            Return .ToString
        End With
    End Function
End Class
