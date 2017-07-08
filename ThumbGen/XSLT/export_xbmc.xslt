<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:msxsl="urn:schemas-microsoft-com:xslt">
  <xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes"/>
  <!--xsl:template match="@*|node()">
      <xsl:copy>
         <xsl:apply-templates select="@*|node()"/>
      </xsl:copy>
   </xsl:template-->
  <xsl:template match="movie">
    <xsl:element name="movie">
      <xsl:element name="title">
        <xsl:value-of select="title"/>
      </xsl:element>
      <xsl:element name="originaltitle">
        <xsl:value-of select="originaltitle"/>
      </xsl:element>
      <xsl:element name="sorttitle">
        <xsl:value-of select="title"/>
      </xsl:element>
      <xsl:element name="plot">
        <xsl:value-of select="plot"/>
      </xsl:element>
      <xsl:element name="year">
        <xsl:value-of select="year"/>
      </xsl:element>
      <xsl:element name="tagline">
        <xsl:value-of select="tagline"/>
      </xsl:element>
      <xsl:element name="releasedate">
        <xsl:value-of select="releasedate"/>
      </xsl:element>
      <xsl:element name="runtime">
        <xsl:value-of select="runtime"/>
      </xsl:element>
      <xsl:element name="rating">
        <xsl:value-of select="rating"/>
      </xsl:element>
      <xsl:element name="mpaa">
        <xsl:value-of select="mpaa"/>
      </xsl:element>
      <xsl:element name="certification">
        <xsl:value-of select="certification"/>
      </xsl:element>
      <xsl:element name="id">
        <xsl:value-of select="id"/>
      </xsl:element>
      <xsl:apply-templates select="genre"/>
      <xsl:apply-templates select="actor"/>
      <xsl:apply-templates select="director"/>
      <xsl:apply-templates select="studio"/>
    </xsl:element>
  </xsl:template>
  <xsl:template match="genre">
    <xsl:element name="genre">
      <xsl:for-each select="name">
        <xsl:value-of select="."/>
        <xsl:if test="position()!=last()">
          <xsl:text> / </xsl:text>
        </xsl:if>
      </xsl:for-each>
    </xsl:element>
  </xsl:template>
  <xsl:template match="actor">
    <xsl:for-each select="name">
      <xsl:element name="actor">
        <xsl:element name="name">
          <xsl:value-of select=".">
          </xsl:value-of>
        </xsl:element>
      </xsl:element>
    </xsl:for-each>
  </xsl:template>
  <xsl:template match="director">
    <xsl:element name="director">
      <xsl:for-each select="name">
        <xsl:value-of select="."/>
        <xsl:if test="position()!=last()">
          <xsl:text> / </xsl:text>
        </xsl:if>
      </xsl:for-each>
    </xsl:element>
  </xsl:template>
  <xsl:template match="studio">
    <xsl:element name="studio">
      <xsl:for-each select="name">
        <xsl:value-of select="."/>
        <xsl:if test="position()!=last()">
          <xsl:text> / </xsl:text>
        </xsl:if>
      </xsl:for-each>
    </xsl:element>
  </xsl:template>
</xsl:stylesheet>
