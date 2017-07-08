<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl">
  <xsl:output method="xml" indent="yes"/>
  <xsl:template match="movie">
    <xsl:element name="movie">
      <xsl:element name="title">
        <xsl:value-of select="title"/>
      </xsl:element>
      <xsl:element name="originaltitle">
        <xsl:value-of select="originaltitle"/>
      </xsl:element>
      <xsl:element name="year">
        <xsl:value-of select="year"/>
      </xsl:element>
      <xsl:element name="plot">
        <xsl:value-of select="plot"/>
      </xsl:element>
      <xsl:element name="tagline">
        <xsl:value-of select="tagline"/>
      </xsl:element>
      <xsl:element name="comments">
        <xsl:value-of select="outline"/>
      </xsl:element>
      <xsl:element name="rating">
        <xsl:value-of select="rating"/>
      </xsl:element>
      <xsl:element name="releasedate">
        <xsl:value-of select="releasedate"/>
      </xsl:element>
      <xsl:element name="id">
        <xsl:value-of select="id"/>
      </xsl:element>
      <xsl:element name="actor">
        <xsl:call-template name="actor"/>
      </xsl:element>
      <xsl:element name="genre">
        <xsl:call-template name="genre"/>
      </xsl:element>
      <xsl:element name="director">
        <xsl:element name="name">
          <xsl:value-of select="director"/>
        </xsl:element>
      </xsl:element>
      <xsl:element name="runtime">
        <xsl:value-of select="runtime"/>
      </xsl:element>
      <xsl:element name="releasedate">
        <xsl:value-of select="releasedate"/>
      </xsl:element>
      <xsl:element name="mpaa">
        <xsl:value-of select="mpaa"/>
      </xsl:element>
      <xsl:element name="certification">
        <xsl:call-template name="certification"/>
      </xsl:element>
      <xsl:element name="studio">
        <xsl:element name="name">
          <xsl:value-of select="studio"/>
        </xsl:element>
      </xsl:element>
      <xsl:element name="country">
        <xsl:element name="name">
          <xsl:value-of select="country"/>
        </xsl:element>
      </xsl:element>
    </xsl:element>
  </xsl:template>
  <xsl:template name="actor">
    <xsl:for-each select="actor">
      <xsl:element name="name">
        <xsl:value-of select="name"/>
      </xsl:element>
    </xsl:for-each>
  </xsl:template>
  <xsl:template name="certification">
  </xsl:template>

  <xsl:template name="genre">
    <!--<xsl:analyze-string select="genre" regex="(\b\S[^/]*)">
			<xsl:matching-substring>
				<xsl:element name="name">
					<xsl:value-of select="regex-group(1)"/>
				</xsl:element>
			</xsl:matching-substring>
		</xsl:analyze-string>-->
    <xsl:call-template name="divide">
      <xsl:with-param name="to-be-divided" select="genre"/>
      <xsl:with-param name="delimiter" select="'/'"/>
    </xsl:call-template>
  </xsl:template>
  <xsl:template name="divide">
    <xsl:param name="to-be-divided"/>
    <xsl:param name="delimiter"/>
    <xsl:choose>
      <xsl:when test="contains($to-be-divided,$delimiter)">
        <name>
          <xsl:value-of select="translate(substring-before($to-be-divided,$delimiter) ,' ', '') "/>
        </name>
        <xsl:call-template name="divide">
          <xsl:with-param name="to-be-divided" select="substring-after($to-be-divided,$delimiter)"/>
          <xsl:with-param name="delimiter" select="'/'"/>
        </xsl:call-template>
      </xsl:when>
      <xsl:otherwise>
        <name>
          <xsl:value-of select="translate($to-be-divided, ' ', '' )"/>
        </name>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
</xsl:stylesheet>


