<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl">
  <xsl:output method="xml" indent="yes"/>
  <xsl:template match="//AntMovieCatalog/Catalog/Contents">
    <xsl:element name="movies">
      <xsl:call-template name="movie"/>
    </xsl:element>
  </xsl:template>
  <xsl:template name="movie">
    <xsl:for-each select="Movie">
      <xsl:element name="movie">
        <xsl:attribute name="ThumbGen">
          <xsl:text>1</xsl:text>
        </xsl:attribute>
        <xsl:element name="_movieid">
          <xsl:value-of select="@Number"/>
        </xsl:element>
        <xsl:element name="title">
          <xsl:value-of select="@TranslatedTitle"/>
        </xsl:element>
        <xsl:element name="originaltitle">
          <xsl:value-of select="@OriginalTitle"/>
        </xsl:element>
        <xsl:element name="year">
          <xsl:value-of select="@Year"/>
        </xsl:element>
        <xsl:element name="plot">
          <xsl:value-of select="@Description"/>
        </xsl:element>
        <xsl:element name="comments">
          <xsl:value-of select="@Comments"/>
        </xsl:element>
        <xsl:element name="rating">
          <xsl:value-of select="@Rating"/>
        </xsl:element>
        <xsl:element name="releasedate">

        </xsl:element>
        <xsl:element name="id">
          <xsl:call-template name="extract">
            <xsl:with-param name="extract-from" select="@URL"/>
            <xsl:with-param name="start-with" select="'/tt'"/>
          </xsl:call-template>
        </xsl:element>
        <xsl:element name="actor">
          <xsl:call-template name="actor"/>
        </xsl:element>
        <xsl:element name="genre">
          <xsl:call-template name="genre"/>
        </xsl:element>
        <xsl:element name="director">
          <xsl:call-template name="director"/>
        </xsl:element>
        <xsl:element name="runtime">
          <xsl:value-of select="@Length"/>
        </xsl:element>
        <xsl:element name="mpaa">

        </xsl:element>
        <xsl:element name="certification">

        </xsl:element>
        <xsl:element name="studio">
          <xsl:call-template name="studio"/>
        </xsl:element>
        <xsl:element name="country">
          <xsl:call-template name="country"/>
        </xsl:element>
        <xsl:element name="cover">
          <xsl:value-of select="@Picture"/>
        </xsl:element>
        <xsl:element name="localfiles">
          <xsl:element name="name">
            <xsl:value-of select="@URL"/>
          </xsl:element>
        </xsl:element>
      </xsl:element>
    </xsl:for-each>
  </xsl:template>
  <xsl:template name="director">
    <xsl:call-template name="divide">
      <xsl:with-param name="to-be-divided" select="@Director"/>
      <xsl:with-param name="delimiter" select="', '"/>
    </xsl:call-template>
  </xsl:template>
  <xsl:template name="genre">
    <xsl:call-template name="divide">
      <xsl:with-param name="to-be-divided" select="@Category"/>
      <xsl:with-param name="delimiter" select="', '"/>
    </xsl:call-template>
  </xsl:template>
  <xsl:template name="studio">
    <xsl:call-template name="divide">
      <xsl:with-param name="to-be-divided" select="@Producer"/>
      <xsl:with-param name="delimiter" select="', '"/>
    </xsl:call-template>
  </xsl:template>
  <xsl:template name="country">
    <xsl:call-template name="divide">
      <xsl:with-param name="to-be-divided" select="@Country"/>
      <xsl:with-param name="delimiter" select="', '"/>
    </xsl:call-template>
  </xsl:template>

  <xsl:template name="actor">
    <xsl:call-template name="divide">
      <xsl:with-param name="to-be-divided" select="@Actors"/>
      <xsl:with-param name="delimiter" select="', '"/>
    </xsl:call-template>
  </xsl:template>

  <xsl:template name="extract">
    <xsl:param name="extract-from"/>
    <xsl:param name="start-with"/>
    <xsl:choose>
      <xsl:when test="contains($extract-from, $start-with)">
        <xsl:variable name="tempvalue">
          <xsl:value-of select="concat('tt', substring-after($extract-from, $start-with))"/>
        </xsl:variable>
        <xsl:choose>
          <xsl:when test="string-length($tempvalue) > 2">
            <xsl:value-of select="$tempvalue"/>
          </xsl:when>
        </xsl:choose>
      </xsl:when>
    </xsl:choose>
  </xsl:template>

  <xsl:template name="divide">
    <xsl:param name="to-be-divided"/>
    <xsl:param name="delimiter"/>
    <xsl:choose>
      <xsl:when test="contains($to-be-divided,$delimiter)">
        <name>
          <xsl:value-of select="translate(substring-before($to-be-divided,$delimiter) ,' ', ' ') "/>
        </name>
        <xsl:call-template name="divide">
          <xsl:with-param name="to-be-divided" select="substring-after($to-be-divided,$delimiter)"/>
          <xsl:with-param name="delimiter" select="$delimiter"/>
        </xsl:call-template>
      </xsl:when>
      <xsl:otherwise>
        <name>
          <xsl:value-of select="translate($to-be-divided, ' ', ' ' )"/>
        </name>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
</xsl:stylesheet>
